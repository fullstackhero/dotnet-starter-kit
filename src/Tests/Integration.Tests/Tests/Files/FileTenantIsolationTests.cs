using System.Security.Cryptography;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// Proves the Files module never leaks a file across tenant boundaries. FileAsset carries no
/// TenantId column — isolation is enforced by the framework's schema-per-tenant BaseDbContext, so a
/// lookup from another tenant simply misses the row and the handler 404s ("file not found"). These
/// tests stand up a second tenant (root + provisioned tenant) and assert that tenant B can neither
/// read metadata nor mint a download URL for a file uploaded in tenant A (root).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class FileTenantIsolationTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public FileTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    #region Cross-tenant isolation

    [Fact]
    public async Task GetMetadata_Should_Return404_When_File_Owned_By_Different_Tenant()
    {
        // Arrange — root uploads a Private file; a second tenant comes online.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(rootClient, "tenant-a-secret.pdf", "application/pdf", 256, visibility: 1);

        using var otherClient = await CreateOtherTenantAdminClientAsync();

        // Act — tenant B asks for tenant A's file metadata.
        using var response = await otherClient.GetAsync($"{FilesBasePath}/{fileId}");

        // Assert — the row lives in a different schema, so the lookup misses → 404 (no existence leak).
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDownloadUrl_Should_Return404_When_File_Owned_By_Different_Tenant()
    {
        // Arrange — even a Public file in tenant A must be invisible to tenant B; visibility is a
        // within-tenant concept, the schema boundary comes first.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(rootClient, "tenant-a-public.pdf", "application/pdf", 256, visibility: 0);

        using var otherClient = await CreateOtherTenantAdminClientAsync();

        // Act — tenant B tries to mint a presigned GET for tenant A's file.
        using var response = await otherClient.GetAsync($"{FilesBasePath}/{fileId}/url");

        // Assert — no row in tenant B's schema → 404, no presigned URL handed out.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMetadata_Should_Return_File_For_Owning_Tenant_While_Other_Tenant_Gets_404()
    {
        // Arrange — same file, two tenants. This locks the positive AND negative halves together so
        // the 404 above can't be a false-positive from a broken upload.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(rootClient, "shared-id.pdf", "application/pdf", 256, visibility: 1);

        using var otherClient = await CreateOtherTenantAdminClientAsync();

        // Act
        using var ownerResponse = await rootClient.GetAsync($"{FilesBasePath}/{fileId}");
        using var crossResponse = await otherClient.GetAsync($"{FilesBasePath}/{fileId}");

        // Assert — owning tenant sees it, the other tenant does not.
        ownerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await ownerResponse.DeserializeAsync<FileAssetDto>();
        dto.Id.ShouldBe(fileId);

        crossResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Tenant scaffolding (copied from WebhookTenantIsolationTests)

    private async Task<HttpClient> CreateOtherTenantAdminClientAsync()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"file-iso-{uniqueId}";
        var otherAdminEmail = $"file-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        return await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        var finalResponse = await client.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }

    #endregion

    #region Upload helpers

    private static async Task<Guid> UploadAndFinalizeAsync(
        HttpClient client,
        string fileName,
        string contentType,
        int sizeBytes,
        int visibility)
    {
        byte[] bytes = new byte[sizeBytes];
        RandomNumberGenerator.Fill(bytes);

        using var urlResponse = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName,
            contentType,
            sizeBytes,
            visibility,
            category = "Document"
        });
        urlResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var presigned = await urlResponse.DeserializeAsync<PresignedUploadResponse>();

        using var raw = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue(contentType) }
            }
        };
        using var putResp = await raw.SendAsync(put);
        putResp.EnsureSuccessStatusCode();

        using var finalize = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        finalize.EnsureSuccessStatusCode();
        return presigned.FileAssetId;
    }

    #endregion
}
