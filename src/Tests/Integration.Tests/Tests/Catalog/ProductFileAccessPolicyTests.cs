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

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// Exercises <c>ProductFileAccessPolicy</c> (OwnerType=Product) through the Files endpoints that
/// resolve it: upload-url (CanAttach), download-url (CanRead), and delete (CanDelete). The policy
/// keeps reads open and limits delete to the uploader, so a second user must be blocked from
/// deleting an admin-uploaded product image. Mirrors the file-access-policy testing approach in
/// <see cref="Chat.ChatChannelFileAccessTests"/>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ProductFileAccessPolicyTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ProductFileAccessPolicyTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── CanAttach ───────────────────────────────────────────────────

    [Fact]
    public async Task RequestUploadUrl_For_Product_Should_Succeed_For_Authenticated_User()
    {
        // CanAttachAsync returns true for any authenticated user.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "Product",
            ownerId = productId,
            fileName = "test.png",
            contentType = "image/png",
            sizeBytes = 2048,
            visibility = 0,
            category = "Image",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ─── CanRead ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetDownloadUrl_For_Product_Image_Should_Succeed_For_Any_Authenticated_User()
    {
        // CanReadAsync is open: a non-uploader can still mint a download URL for a product image.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(adminClient);
        var productId = await CreateProductAsync(adminClient, brandId, categoryId);
        var fileAssetId = await UploadImageAsync(adminClient, productId);

        var (otherEmail, otherPassword) = await RegisterAndConfirmAsync(adminClient, "reader");
        using var otherClient = await _auth.CreateAuthenticatedClientAsync(otherEmail, otherPassword);

        using var response = await otherClient.GetAsync($"{FilesBasePath}/{fileAssetId}/url");

        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            "product images are publicly readable, so any authenticated user may mint a download URL");
        var download = await response.DeserializeAsync<PresignedDownloadResponse>();
        download.Url.ShouldNotBeNull();
    }

    // ─── CanDelete ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteFile_For_Product_Image_Should_Succeed_For_Uploader()
    {
        // CanDeleteAsync allows the uploader. Admin uploads, admin deletes.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);
        var fileAssetId = await UploadImageAsync(client, productId);

        using var response = await client.DeleteAsync($"{FilesBasePath}/{fileAssetId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteFile_For_Product_Image_Should_Return403_For_NonUploader()
    {
        // CanDeleteAsync is uploader-only — a different user is forbidden even with Files.DeleteOwn.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(adminClient);
        var productId = await CreateProductAsync(adminClient, brandId, categoryId);
        var fileAssetId = await UploadImageAsync(adminClient, productId);

        var (otherEmail, otherPassword) = await RegisterAndConfirmAsync(adminClient, "deleter");
        using var otherClient = await _auth.CreateAuthenticatedClientAsync(otherEmail, otherPassword);

        using var response = await otherClient.DeleteAsync($"{FilesBasePath}/{fileAssetId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<(Guid BrandId, Guid CategoryId)> CreateBrandAndCategoryAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var brandResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/brands",
            new { name = $"fap-brand-{suffix}", description = (string?)null, logoUrl = (string?)null });
        brandResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create brand: {await brandResp.Content.ReadAsStringAsync()}");
        var brandId = await brandResp.DeserializeAsync<Guid>();

        using var categoryResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories",
            new { name = $"fap-cat-{suffix}", description = (string?)null, parentCategoryId = (Guid?)null });
        categoryResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create category: {await categoryResp.Content.ReadAsStringAsync()}");
        var categoryId = await categoryResp.DeserializeAsync<Guid>();

        return (brandId, categoryId);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, Guid brandId, Guid categoryId)
    {
        var sku = "FAP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        using var createResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = $"Policy product {sku}",
                description = "integration-test product",
                brandId,
                categoryId,
                priceAmount = 19.99m,
                priceCurrency = "USD",
                stock = 10
            });
        if (createResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await createResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"CreateProduct failed: {createResp.StatusCode}\n{body}");
        }
        return await createResp.DeserializeAsync<Guid>();
    }

    /// <summary>Upload + finalize a Product-owned image and return its FileAssetId.</summary>
    private static async Task<Guid> UploadImageAsync(HttpClient client, Guid productId)
    {
        byte[] bytes = new byte[2048];
        RandomNumberGenerator.Fill(bytes);

        using var presignedResp = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "Product",
            ownerId = productId,
            fileName = "test.png",
            contentType = "image/png",
            sizeBytes = bytes.Length,
            visibility = 0,
            category = "Image",
        });
        if (presignedResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await presignedResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"upload-url failed: {presignedResp.StatusCode}\n{body}");
        }
        var presigned = await presignedResp.DeserializeAsync<PresignedUploadResponse>();

        using var raw = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
            }
        };
        using var putResp = await raw.SendAsync(put);
        putResp.EnsureSuccessStatusCode();

        using var finalizeResp = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        finalizeResp.EnsureSuccessStatusCode();

        return presigned.FileAssetId;
    }

    /// <summary>
    /// Register a second user under the root tenant and confirm their email so they can log in.
    /// Mirrors <see cref="Chat.ChatChannelFileAccessTests"/>: the Finbuckle tenant context is set
    /// inline inside the DI scope to avoid the AsyncLocal NRE in the tenant query filter.
    /// </summary>
    private async Task<(string email, string password)> RegisterAndConfirmAsync(HttpClient adminClient, string prefix)
    {
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

        using var scope = _factory.Services.CreateScope();
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

        return (email, password);
    }
}
