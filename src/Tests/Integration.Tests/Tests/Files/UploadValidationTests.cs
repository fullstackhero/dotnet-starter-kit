using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Files;

[Collection(FshCollectionDefinition.Name)]
public sealed class UploadValidationTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public UploadValidationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task UploadUrl_Should_Return400_When_Extension_Not_Allowed_For_Category()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // .exe isn't in Document's allowed list (only pdf/docx/xlsx/pptx/txt/csv).
        using var response = await PostUploadUrlAsync(client, fileName: "evil.exe", contentType: "application/octet-stream", sizeBytes: 100, category: "Document");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadUrl_Should_Return400_When_SizeBytes_Exceeds_Category_Max()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // Image category max is 10 MiB. 50 MiB busts the cap.
        using var response = await PostUploadUrlAsync(client, fileName: "huge.png", contentType: "image/png", sizeBytes: 50L * 1024 * 1024, category: "Image");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadUrl_Should_Return400_When_Category_Is_Unknown()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await PostUploadUrlAsync(client, fileName: "doc.pdf", contentType: "application/pdf", sizeBytes: 256, category: "NotARealCategory");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadUrl_Should_Return400_When_FileName_Is_Empty()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await PostUploadUrlAsync(client, fileName: "", contentType: "application/pdf", sizeBytes: 256, category: "Document");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadUrl_Should_Return403_When_OwnerType_Has_No_Registered_Policy()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await PostUploadUrlAsync(
            client,
            fileName: "doc.pdf",
            contentType: "application/pdf",
            sizeBytes: 256,
            category: "Document",
            ownerType: "SomethingNoOneRegistered");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static Task<HttpResponseMessage> PostUploadUrlAsync(
        HttpClient client,
        string fileName,
        string contentType,
        long sizeBytes,
        string category,
        string ownerType = "MyFiles",
        int visibility = 1)
    {
        return client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType,
            ownerId = (Guid?)null,
            fileName,
            contentType,
            sizeBytes,
            visibility,
            category
        });
    }
}
