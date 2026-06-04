using System.Security.Cryptography;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// Covers the file visibility/sharing model (shipped 2026-05-21). The Files module has no
/// per-grantee ACL — "sharing" is a Public ↔ Private bit enforced by <c>DefaultUploaderOnlyPolicy</c>
/// for the built-in <c>MyFiles</c>/<c>User</c> owner types:
/// <list type="bullet">
///   <item>Private (visibility=1): readable only by the uploader. A non-owner in the same tenant
///   gets 404 on both GET metadata and GET /url (existence is not leaked).</item>
///   <item>Public (visibility=0): readable by anyone in the tenant. A non-owner can read metadata,
///   mint a download URL, and see the file in the tenant-wide <c>/shared</c> list.</item>
///   <item>Flipping the bit (<c>PATCH /{id}/visibility</c>) is uploader-only — a non-owner gets 403.</item>
/// </list>
/// Tests use two users in the SAME tenant (owner = root admin, grantee = a freshly registered user)
/// plus the grantee acting as a non-owner.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class FileVisibilityAndSharingTests
{
    private const string FilesBasePath = "/api/v1/files";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public FileVisibilityAndSharingTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Private files are owner-only

    [Fact]
    public async Task GetMetadata_Should_Return404_For_NonOwner_When_File_Is_Private()
    {
        // Arrange — owner uploads a Private file; a different same-tenant user exists.
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "private.pdf", "application/pdf", 256, visibility: 1);

        using var granteeClient = await RegisterAndLoginAsync("private-reader");

        // Act — the non-owner asks for the Private file's metadata.
        using var response = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}");

        // Assert — Private + non-owner → 404 (CanReadAsync false; existence not leaked).
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDownloadUrl_Should_Return404_For_NonOwner_When_File_Is_Private()
    {
        // Arrange
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "private-dl.pdf", "application/pdf", 256, visibility: 1);

        using var granteeClient = await RegisterAndLoginAsync("private-dl-reader");

        // Act — non-owner tries to mint a presigned GET for a Private file.
        using var response = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}/url");

        // Assert — no download URL for a non-owner on a Private file.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMetadata_Should_Return_File_For_Owner_When_File_Is_Private()
    {
        // Arrange — positive control: the uploader can always read their own Private file.
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "owner-private.pdf", "application/pdf", 256, visibility: 1);

        // Act
        using var response = await ownerClient.GetAsync($"{FilesBasePath}/{fileId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.DeserializeAsync<FileAssetDto>();
        dto.Id.ShouldBe(fileId);
        dto.Visibility.ShouldBe(Visibility.Private);
    }

    #endregion

    #region Public (shared) files are reachable by any same-tenant user

    [Fact]
    public async Task GetMetadata_Should_Return_File_For_NonOwner_When_File_Is_Public()
    {
        // Arrange — a Public file is "shared" with the whole tenant.
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "public.pdf", "application/pdf", 256, visibility: 0);

        using var granteeClient = await RegisterAndLoginAsync("public-reader");

        // Act — a different same-tenant user reads the Public file's metadata.
        using var response = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}");

        // Assert — Public files are readable by anyone in the tenant.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.DeserializeAsync<FileAssetDto>();
        dto.Id.ShouldBe(fileId);
        dto.Visibility.ShouldBe(Visibility.Public);
    }

    [Fact]
    public async Task GetDownloadUrl_Should_Succeed_For_NonOwner_When_File_Is_Public()
    {
        // Arrange
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "public-dl.pdf", "application/pdf", 256, visibility: 0);

        using var granteeClient = await RegisterAndLoginAsync("public-dl-reader");

        // Act — non-owner mints a presigned GET for the Public file.
        using var response = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}/url");

        // Assert — Public read is allowed; a real presigned URL comes back.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var download = await response.DeserializeAsync<PresignedDownloadResponse>();
        download.Url.ShouldNotBeNull();
        download.Url.AbsoluteUri.ShouldContain("X-Amz-Signature");
    }

    [Fact]
    public async Task ListShared_Should_Include_Public_File_For_NonOwner()
    {
        // Arrange — the /shared surface lists Public, Available MyFiles/User files tenant-wide.
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "in-shared-view.pdf", "application/pdf", 256, visibility: 0);

        using var granteeClient = await RegisterAndLoginAsync("shared-lister");

        // Act — a different same-tenant user lists the shared view.
        using var response = await granteeClient.GetAsync($"{FilesBasePath}/shared?page=1&pageSize=100");

        // Assert — the Public file is visible in another user's shared list.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.DeserializeAsync<IReadOnlyList<FileAssetDto>>();
        list.ShouldContain(f => f.Id == fileId);
    }

    [Fact]
    public async Task ListShared_Should_Not_Include_Private_File()
    {
        // Arrange — a Private file must never surface in the tenant-wide shared view, even for the owner.
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var privateId = await UploadAndFinalizeAsync(ownerClient, "not-shared.pdf", "application/pdf", 256, visibility: 1);

        // Act
        using var response = await ownerClient.GetAsync($"{FilesBasePath}/shared?page=1&pageSize=100");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.DeserializeAsync<IReadOnlyList<FileAssetDto>>();
        list.ShouldNotContain(f => f.Id == privateId);
    }

    #endregion

    #region Sharing = flipping visibility (uploader-only)

    [Fact]
    public async Task ChangeVisibility_Should_Make_Private_File_Reachable_By_NonOwner()
    {
        // Arrange — start Private (non-owner is locked out), then the owner shares it (→ Public).
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "to-share.pdf", "application/pdf", 256, visibility: 1);

        using var granteeClient = await RegisterAndLoginAsync("share-target");

        using var before = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}");
        before.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Private file must be hidden from a non-owner before sharing.");

        // Act — owner flips it to Public (visibility 0).
        using var flip = await ownerClient.PatchAsJsonAsync($"{FilesBasePath}/{fileId}/visibility", new { visibility = 0 });
        flip.StatusCode.ShouldBe(HttpStatusCode.OK);
        var flipped = await flip.DeserializeAsync<FileAssetDto>();
        flipped.Visibility.ShouldBe(Visibility.Public);

        // Assert — the same non-owner can now read it.
        using var after = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}");
        after.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await after.DeserializeAsync<FileAssetDto>();
        dto.Id.ShouldBe(fileId);
    }

    [Fact]
    public async Task ChangeVisibility_Should_Return403_When_Caller_Is_Not_The_Uploader()
    {
        // Arrange — a non-owner must not be able to flip another user's file visibility.
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "owner-controls.pdf", "application/pdf", 256, visibility: 0);

        using var granteeClient = await RegisterAndLoginAsync("not-the-uploader");

        // Act — non-owner tries to flip Public → Private.
        using var response = await granteeClient.PatchAsJsonAsync(
            $"{FilesBasePath}/{fileId}/visibility", new { visibility = 1 });

        // Assert — CanChangeVisibilityAsync defaults to uploader-only → 403 Forbidden.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeVisibility_Should_Hide_Public_File_From_NonOwner_When_Flipped_To_Private()
    {
        // Arrange — start Public (non-owner can read), then the owner un-shares it (→ Private).
        using var ownerClient = await _auth.CreateRootAdminClientAsync();
        var fileId = await UploadAndFinalizeAsync(ownerClient, "unshare.pdf", "application/pdf", 256, visibility: 0);

        using var granteeClient = await RegisterAndLoginAsync("unshare-target");

        using var before = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}");
        before.StatusCode.ShouldBe(HttpStatusCode.OK, "Public file must be readable by a non-owner before un-sharing.");

        // Act — owner flips it back to Private.
        using var flip = await ownerClient.PatchAsJsonAsync($"{FilesBasePath}/{fileId}/visibility", new { visibility = 1 });
        flip.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert — the non-owner loses access.
        using var after = await granteeClient.GetAsync($"{FilesBasePath}/{fileId}");
        after.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Registers a fresh user in the root tenant (via the root-admin client), confirms their email,
    /// and returns an authenticated client for them. A loginable user needs EmailConfirmed = true;
    /// registration already sets IsActive = true (UserRegistrationService).
    /// </summary>
    private async Task<HttpClient> RegisterAndLoginAsync(string prefix)
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@example.com";
        var userName = $"{prefix}{unique}";
        const string password = "Test@1234!";

        using var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = prefix,
            lastName = "Test",
            email,
            userName,
            password,
            confirmPassword = password,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await response.DeserializeAsync<RegisterResult>();

        // Bypass email confirmation so the user can sign in. The Finbuckle tenant context must be set
        // INLINE in this method body (AsyncLocal) so the UserManager query filter resolves the tenant.
        using (var scope = _factory.Services.CreateScope())
        {
            var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
                .GetAsync(TestConstants.RootTenantId);
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
                new MultiTenantContext<AppTenantInfo>(tenant);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
            var user = await userManager.FindByIdAsync(registered.UserId);
            user.ShouldNotBeNull();
            if (!user!.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                (await userManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password);
    }

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
