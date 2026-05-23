using System.Security.Cryptography;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// Exercises more of the S3 storage surface (S3StorageService) end-to-end through the Files
/// endpoints: presigned-GET disposition overrides (inline vs attachment + original filename echo)
/// and a real round-trip download that proves the bytes uploaded via the presigned PUT are served
/// back intact by the presigned GET. Does not duplicate the existing finalize / soft-delete /
/// visibility scenarios — it focuses on the download path and storage byte fidelity.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class StorageFlowTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public StorageFlowTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task DownloadUrl_Should_RoundTrip_The_Exact_Bytes_That_Were_Uploaded()
    {
        // Arrange — upload known bytes through the presigned PUT, then finalize.
        using var client = await _auth.CreateRootAdminClientAsync();
        byte[] bytes = new byte[2048];
        RandomNumberGenerator.Fill(bytes);
        var id = await UploadAndFinalizeAsync(client, "roundtrip.pdf", "application/pdf", bytes);

        // Act — mint a presigned GET and fetch the bytes straight from MinIO.
        using var urlResp = await client.GetAsync($"{FilesBasePath}/{id}/url");
        urlResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var download = await urlResp.DeserializeAsync<PresignedDownloadResponse>();

        using var raw = new HttpClient();
        using var getResp = await raw.GetAsync(download.Url);

        // Assert — the bytes match exactly (presigned GET serves what the presigned PUT stored).
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await getResp.Content.ReadAsByteArrayAsync();
        fetched.ShouldBe(bytes);
    }

    [Fact]
    public async Task DownloadUrl_Should_Request_Attachment_Disposition_With_Original_Filename_By_Default()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "report-final.pdf", "application/pdf", RandomBytes(512));

        // Default (no ?inline) → S3 echoes Content-Disposition: attachment; filename="report-final.pdf".
        using var urlResp = await client.GetAsync($"{FilesBasePath}/{id}/url");
        var download = await urlResp.DeserializeAsync<PresignedDownloadResponse>();

        using var raw = new HttpClient();
        using var getResp = await raw.GetAsync(download.Url);
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var disposition = getResp.Content.Headers.ContentDisposition;
        disposition.ShouldNotBeNull();
        disposition!.DispositionType.ShouldBe("attachment");
        // The original filename is echoed back so the browser surfaces it instead of the storage key.
        disposition.FileName!.Trim('"').ShouldBe("report-final.pdf");
    }

    [Fact]
    public async Task DownloadUrl_Should_Request_Inline_Disposition_When_Inline_True()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "preview.pdf", "application/pdf", RandomBytes(512));

        using var urlResp = await client.GetAsync($"{FilesBasePath}/{id}/url?inline=true");
        urlResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var download = await urlResp.DeserializeAsync<PresignedDownloadResponse>();

        using var raw = new HttpClient();
        using var getResp = await raw.GetAsync(download.Url);
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var disposition = getResp.Content.Headers.ContentDisposition;
        disposition.ShouldNotBeNull();
        disposition!.DispositionType.ShouldBe("inline");
    }

    #endregion

    // ─── helpers ─────────────────────────────────────────────────────

    private static byte[] RandomBytes(int size)
    {
        byte[] bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    private static async Task<Guid> UploadAndFinalizeAsync(
        HttpClient client, string fileName, string contentType, byte[] bytes)
    {
        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName,
            contentType,
            sizeBytes = bytes.Length,
            visibility = 1,
            category = "Document",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var presigned = await response.DeserializeAsync<PresignedUploadResponse>();

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
