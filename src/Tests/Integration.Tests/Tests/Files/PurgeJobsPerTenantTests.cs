using System.Security.Cryptography;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Jobs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// The Files purge jobs are Hangfire recurring jobs registered without a tenant, so they run with no
/// ambient tenant. Purgeable files are seeded in three tenants — root, a tenant on the shared database
/// and a tenant with its own dedicated database — and each job is resolved from a fresh, tenant-less
/// scope (the shape Hangfire's activator gives it) and run once. Every tenant's rows and blobs must be
/// purged, and the deleted-files purge must refund each tenant's storage quota under that tenant's id.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PurgeJobsPerTenantTests
{
    private const string FilesBasePath = "/api/v1/files";
    private const int FileSize = 256;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public PurgeJobsPerTenantTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task PurgeDeletedFilesJob_Should_PurgeAndRefundQuota_InEveryTenant_IncludingDedicatedDatabase()
    {
        // Arrange
        var tenants = await CreateTenantClientsAsync("files-del");
        var quotaStore = new InMemoryQuotaStore();
        var quotas = CreateQuotaService(quotaStore);

        var seeded = new List<(string TenantId, Guid Id, string Key)>();
        foreach (var (tenantId, client) in tenants)
        {
            var (id, key) = await UploadFinalizeAndDeleteAsync(tenantId, client, "purge-me.pdf");
            await BackdateAsync(tenantId, id, deletedOnUtc: DateTimeOffset.UtcNow.AddDays(-90), uploadDeadline: null);

            // Quota is disabled in the harness, so the finalize debit is replayed here against the
            // in-memory counter the job is handed.
            await quotas.RecordAsync(tenantId, QuotaResource.StorageBytes, FileSize);
            seeded.Add((tenantId, id, key));
        }

        // Act — fresh scope, no tenant set, as FshJobActivator gives a job registered via AddOrUpdate.
        using (var jobScope = _factory.Services.CreateScope())
        {
            var job = ActivatorUtilities.CreateInstance<PurgeDeletedFilesJob>(jobScope.ServiceProvider, quotas);
            await job.RunAsync(CancellationToken.None);
        }

        // Assert
        foreach (var (tenantId, id, key) in seeded)
        {
            (await RowExistsIgnoringFiltersAsync(tenantId, id))
                .ShouldBeFalse($"soft-deleted file past retention in tenant '{tenantId}' must be hard-purged");
            (await ObjectExistsAsync(key))
                .ShouldBeFalse($"storage object of the purged file in tenant '{tenantId}' must be removed");
            (await quotas.GetCurrentAsync(tenantId, QuotaResource.StorageBytes))
                .ShouldBe(0, $"the purged bytes must be refunded to tenant '{tenantId}'");
        }

        quotaStore.Counters.Keys.ShouldNotContain(k => k.StartsWith("quota::", StringComparison.Ordinal),
            "no refund may be recorded against an empty tenant id");
    }

    [Fact]
    public async Task PurgeOrphanedFilesJob_Should_PurgeExpiredPendingUploads_InEveryTenant_IncludingDedicatedDatabase()
    {
        // Arrange
        var tenants = await CreateTenantClientsAsync("files-orph");

        var seeded = new List<(string TenantId, Guid Id, string Key)>();
        foreach (var (tenantId, client) in tenants)
        {
            var (id, key) = await RequestUploadAndPutBytesAsync(tenantId, client, "orphan.pdf");
            await BackdateAsync(tenantId, id, deletedOnUtc: null, uploadDeadline: DateTimeOffset.UtcNow.AddHours(-2));
            seeded.Add((tenantId, id, key));
        }

        // Act — fresh scope, no tenant set.
        using (var jobScope = _factory.Services.CreateScope())
        {
            var job = ActivatorUtilities.CreateInstance<PurgeOrphanedFilesJob>(jobScope.ServiceProvider);
            await job.RunAsync(CancellationToken.None);
        }

        // Assert
        foreach (var (tenantId, id, key) in seeded)
        {
            (await RowExistsIgnoringFiltersAsync(tenantId, id))
                .ShouldBeFalse($"expired pending upload in tenant '{tenantId}' must be hard-purged");
            (await ObjectExistsAsync(key))
                .ShouldBeFalse($"orphaned storage object in tenant '{tenantId}' must be removed");
        }
    }

    // ─── tenants ─────────────────────────────────────────────────────

    /// <summary>Root, a shared-database tenant and a dedicated-database tenant, each with an admin client.</summary>
    private async Task<List<(string TenantId, HttpClient Client)>> CreateTenantClientsAsync(string prefix)
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var sharedTenantId = $"{prefix}-shared-{uniqueId}";
        var dedicatedTenantId = $"{prefix}-dedicated-{uniqueId}";

        var defaultConnection = _factory.Services.GetRequiredService<IConfiguration>()["DatabaseOptions:ConnectionString"];
        defaultConnection.ShouldNotBeNullOrWhiteSpace();
        var dedicatedConnection = new NpgsqlConnectionStringBuilder(defaultConnection)
        {
            Database = $"fsh_{prefix.Replace('-', '_')}_{uniqueId}",
        }.ConnectionString;

        await CreateTenantAsync(rootClient, sharedTenantId, connectionString: null);
        await CreateTenantAsync(rootClient, dedicatedTenantId, dedicatedConnection);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, sharedTenantId);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, dedicatedTenantId);

        return
        [
            (TestConstants.RootTenantId, await _auth.CreateRootAdminClientAsync()),
            (sharedTenantId, await _auth.CreateAuthenticatedClientAsync(AdminEmail(sharedTenantId), TestConstants.DefaultPassword, sharedTenantId)),
            (dedicatedTenantId, await _auth.CreateAuthenticatedClientAsync(AdminEmail(dedicatedTenantId), TestConstants.DefaultPassword, dedicatedTenantId)),
        ];
    }

    private static string AdminEmail(string tenantId) => $"admin-{tenantId}@tenant.com";

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string? connectionString)
    {
        using var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString,
            adminEmail = AdminEmail(tenantId),
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static InMemoryQuotaService CreateQuotaService(InMemoryQuotaStore store)
    {
        var options = new QuotaOptions { Enabled = true };
        return new InMemoryQuotaService(store, options, new QuotaPlanResolver(options), [], TimeProvider.System);
    }

    // ─── files ───────────────────────────────────────────────────────

    private async Task<(Guid Id, string StorageKey)> UploadFinalizeAndDeleteAsync(
        string tenantId, HttpClient client, string fileName)
    {
        var (id, storageKey) = await RequestUploadAndPutBytesAsync(tenantId, client, fileName);

        using var finalize = await client.PostAsync($"{FilesBasePath}/{id}/finalize", null);
        finalize.EnsureSuccessStatusCode();

        using var del = await client.DeleteAsync($"{FilesBasePath}/{id}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return (id, storageKey);
    }

    private async Task<(Guid Id, string StorageKey)> RequestUploadAndPutBytesAsync(
        string tenantId, HttpClient client, string fileName)
    {
        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName,
            contentType = "application/pdf",
            sizeBytes = FileSize,
            visibility = 1,
            category = "Document",
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"upload-url failed in tenant '{tenantId}': {body}");
        var presigned = await response.DeserializeAsync<PresignedUploadResponse>();

        byte[] bytes = new byte[FileSize];
        RandomNumberGenerator.Fill(bytes);
        using var raw = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") }
            }
        };
        using var putResp = await raw.SendAsync(put);
        putResp.EnsureSuccessStatusCode();

        var storageKey = await ReadStorageKeyAsync(tenantId, presigned.FileAssetId);
        return (presigned.FileAssetId, storageKey);
    }

    // Tenant context is an AsyncLocal, so it is set in the same method as each DbContext call.

    private async Task<string> ReadStorageKeyAsync(string tenantId, Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        tenant.ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        var key = await db.FileAssets.IgnoreQueryFilters()
            .Where(f => f.Id == id)
            .Select(f => f.StorageKey)
            .FirstOrDefaultAsync();
        key.ShouldNotBeNull();
        return key;
    }

    private async Task BackdateAsync(string tenantId, Guid id, DateTimeOffset? deletedOnUtc, DateTimeOffset? uploadDeadline)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        tenant.ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        var query = db.FileAssets.IgnoreQueryFilters().Where(f => f.Id == id);
        int updated = deletedOnUtc is { } deleted
            ? await query.ExecuteUpdateAsync(s => s.SetProperty(f => f.DeletedOnUtc, deleted))
            : await query.ExecuteUpdateAsync(s => s.SetProperty(f => f.UploadDeadline, uploadDeadline));
        updated.ShouldBe(1);
    }

    private async Task<bool> RowExistsIgnoringFiltersAsync(string tenantId, Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        tenant.ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        return await db.FileAssets.IgnoreQueryFilters().AnyAsync(f => f.Id == id);
    }

    private async Task<bool> ObjectExistsAsync(string storageKey)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IStorageService>().ExistsAsync(storageKey);
    }
}
