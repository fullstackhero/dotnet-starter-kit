using System.Security.Cryptography;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Files;

[Collection(FshCollectionDefinition.Name)]
public sealed class FinalizeEdgeCasesTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public FinalizeEdgeCasesTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Finalize_Should_Return409_When_No_Object_Was_Uploaded()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var presigned = await RequestPresignedUploadAsync(client, "skip-put.pdf", "application/pdf", 256, "Document");

        // Skip the PUT — go straight to finalize.
        using var response = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Finalize_Should_Return409_On_Double_Finalize()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "twice.pdf", "application/pdf", 256, "Document");

        using var response = await client.PostAsync($"{FilesBasePath}/{id}/finalize", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Finalize_Should_Return404_When_FileAsset_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsync($"{FilesBasePath}/{Guid.NewGuid()}/finalize", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Finalize_Should_Return400_And_Cleanup_When_Uploaded_Size_Exceeds_Declared()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // Declare 256 bytes, upload 32 KB. The handler allows declared+1% slack (min 1 KiB), so 32 KB busts it.
        var presigned = await RequestPresignedUploadAsync(client, "lying.pdf", "application/pdf", 256, "Document");

        byte[] bytes = new byte[32 * 1024];
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

        using var finalize = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        finalize.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Subsequent finalize attempts should also fail (record was deleted on cleanup).
        using var second = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        second.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<PresignedUploadResponse> RequestPresignedUploadAsync(
        HttpClient client,
        string fileName,
        string contentType,
        long sizeBytes,
        string category,
        string ownerType = "MyFiles",
        int visibility = 1)
    {
        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType,
            ownerId = (Guid?)null,
            fileName,
            contentType,
            sizeBytes,
            visibility,
            category
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<PresignedUploadResponse>();
    }

    private static async Task<Guid> UploadAndFinalizeAsync(
        HttpClient client,
        string fileName,
        string contentType,
        int sizeBytes,
        string category)
    {
        byte[] bytes = new byte[sizeBytes];
        RandomNumberGenerator.Fill(bytes);

        var presigned = await RequestPresignedUploadAsync(client, fileName, contentType, sizeBytes, category);

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
}
