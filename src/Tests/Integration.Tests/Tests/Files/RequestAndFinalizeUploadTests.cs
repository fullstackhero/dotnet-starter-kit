using System.Security.Cryptography;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Files;

[Collection(FshCollectionDefinition.Name)]
public sealed class RequestAndFinalizeUploadTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public RequestAndFinalizeUploadTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task UploadUrl_Then_Finalize_Should_TransitionToAvailable()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        byte[] bytes = new byte[1024];
        RandomNumberGenerator.Fill(bytes);

        var presigned = await RequestPresignedUploadAsync(client, "doc.pdf", "application/pdf", bytes.Length, "Document");

        // PUT the bytes to MinIO using the presigned URL.
        using var raw = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") }
            }
        };
        using var putResp = await raw.SendAsync(put);
        putResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var finalize = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        finalize.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await finalize.DeserializeAsync<FileAssetDto>();
        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(presigned.FileAssetId);
        dto.Status.ShouldBe(FileAssetStatus.Available);
        dto.SizeBytes.ShouldBe(bytes.Length);
        dto.OriginalFileName.ShouldBe("doc.pdf");
        dto.ContentType.ShouldBe("application/pdf");
    }

    [Fact]
    public async Task GetMetadata_Should_Return_FileAsset_After_Finalize()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "report.pdf", "application/pdf", 512, "Document");

        using var response = await client.GetAsync($"{FilesBasePath}/{id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await response.DeserializeAsync<FileAssetDto>();
        dto.Id.ShouldBe(id);
        dto.Status.ShouldBe(FileAssetStatus.Available);
    }

    [Fact]
    public async Task GetDownloadUrl_Should_Issue_Presigned_Get_Url_For_Available_File()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "download.pdf", "application/pdf", 256, "Document");

        using var response = await client.GetAsync($"{FilesBasePath}/{id}/url");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var download = await response.DeserializeAsync<PresignedDownloadResponse>();
        download.Url.ShouldNotBeNull();
        download.Url.AbsoluteUri.ShouldContain("X-Amz-Signature");
    }

    [Fact]
    public async Task ListMyFiles_Should_Include_Finalized_File()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "mine.pdf", "application/pdf", 128, "Document");

        using var response = await client.GetAsync($"{FilesBasePath}/mine?page=1&pageSize=50");
        var list = await response.DeserializeAsync<IReadOnlyList<FileAssetDto>>();
        list.ShouldNotBeNull();
        list.ShouldContain(f => f.Id == id);
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
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"upload-url failed: {response.StatusCode}\n{body}");
        }

        var presigned = await response.DeserializeAsync<PresignedUploadResponse>();
        presigned.ShouldNotBeNull();
        return presigned;
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
