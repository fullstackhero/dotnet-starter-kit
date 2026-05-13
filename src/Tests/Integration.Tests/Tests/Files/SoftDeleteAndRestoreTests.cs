using System.Security.Cryptography;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Files;

[Collection(FshCollectionDefinition.Name)]
public sealed class SoftDeleteAndRestoreTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public SoftDeleteAndRestoreTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Delete_Should_Soft_Delete_And_Hide_From_GetMetadata()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "to-delete.pdf", "application/pdf", 256, "Document");

        using var deleteResp = await client.DeleteAsync($"{FilesBasePath}/{id}");
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // After soft-delete, GetMetadata should 404 because of the SoftDelete query filter.
        using var getResp = await client.GetAsync($"{FilesBasePath}/{id}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListTrashedFiles_Should_Include_SoftDeleted_File()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "trashed.pdf", "application/pdf", 256, "Document");

        using var deleteResp = await client.DeleteAsync($"{FilesBasePath}/{id}");
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var trashResp = await client.GetAsync($"{FilesBasePath}/trash");
        trashResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var trashed = await trashResp.DeserializeAsync<PagedResponse<FileAssetDto>>();
        trashed.Items.ShouldContain(f => f.Id == id);
    }

    [Fact]
    public async Task Restore_Should_Bring_File_Back_From_Trash()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "restorable.pdf", "application/pdf", 256, "Document");

        using var deleteResp = await client.DeleteAsync($"{FilesBasePath}/{id}");
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var restoreResp = await client.PostAsync($"{FilesBasePath}/{id}/restore", null);
        restoreResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // After restore, GetMetadata should return the file again.
        using var getResp = await client.GetAsync($"{FilesBasePath}/{id}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await getResp.DeserializeAsync<FileAssetDto>();
        dto.Id.ShouldBe(id);
    }

    [Fact]
    public async Task Delete_Should_Return404_When_File_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.DeleteAsync($"{FilesBasePath}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restore_Should_Return404_When_File_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsync($"{FilesBasePath}/{Guid.NewGuid()}/restore", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<PresignedUploadResponse> RequestPresignedUploadAsync(
        HttpClient client,
        string fileName,
        string contentType,
        long sizeBytes,
        string category)
    {
        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName,
            contentType,
            sizeBytes,
            visibility = 1,
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
