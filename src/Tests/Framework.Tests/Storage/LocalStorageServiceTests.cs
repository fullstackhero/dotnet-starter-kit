using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Local;
using Microsoft.AspNetCore.Hosting;

namespace Framework.Tests.Storage;

public sealed class LocalStorageServiceTests : IDisposable
{
    private sealed class Probe { }

    private readonly string _root;
    private readonly LocalStorageService _sut;

    public LocalStorageServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "fsh-local-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        var environment = Substitute.For<IWebHostEnvironment>();
        environment.WebRootPath.Returns(_root);
        environment.ContentRootPath.Returns(_root);

        _sut = new LocalStorageService(environment);
    }

    private static FileUploadRequest PngRequest(string fileName = "avatar.png")
        => new()
        {
            FileName = fileName,
            ContentType = "image/png",
            Data = new List<byte> { 1, 2, 3, 4 }
        };

    #region Happy Path

    [Fact]
    public async Task UploadAsync_Should_PersistFileAndReturnRelativePath_When_ValidImage()
    {
        // Arrange
        var request = PngRequest();

        // Act
        var path = await _sut.UploadAsync<Probe>(request, FileType.Image);

        // Assert
        path.ShouldStartWith("uploads/probe/");
        path.ShouldContain("_avatar.png");
        path.ShouldNotContain("\\");
        File.Exists(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar))).ShouldBeTrue();
    }

    [Fact]
    public async Task UploadDownloadExists_Should_RoundTrip_When_FileUploaded()
    {
        // Arrange
        var request = PngRequest();

        // Act
        var path = await _sut.UploadAsync<Probe>(request, FileType.Image);
        var exists = await _sut.ExistsAsync(path);
        var size = await _sut.GetSizeAsync(path);
        var download = await _sut.DownloadAsync(path);

        // Assert
        exists.ShouldBeTrue();
        size.ShouldBe(4);
        download.ShouldNotBeNull();
        download!.ContentType.ShouldBe("image/png");
        download.ContentLength.ShouldBe(4);
        await download.Stream.DisposeAsync();
    }

    [Fact]
    public async Task RemoveAsync_Should_DeleteFile_When_FileExists()
    {
        // Arrange
        var path = await _sut.UploadAsync<Probe>(PngRequest(), FileType.Image);
        var diskPath = path.Replace('/', Path.DirectorySeparatorChar);

        // Act
        await _sut.RemoveAsync(diskPath);

        // Assert
        (await _sut.ExistsAsync(path)).ShouldBeFalse();
    }

    [Fact]
    public async Task HeadObjectAsync_Should_ReturnMetadata_When_FileExists()
    {
        // Arrange
        var path = await _sut.UploadAsync<Probe>(PngRequest(), FileType.Image);

        // Act
        var metadata = await _sut.HeadObjectAsync(path);

        // Assert
        metadata.ShouldNotBeNull();
        metadata!.SizeBytes.ShouldBe(4);
        metadata.ContentType.ShouldBe("image/png");
    }

    #endregion

    #region URL generation

    [Fact]
    public async Task GenerateUploadUrlAsync_Should_ReturnLocalTokenUrl_When_KeyProvided()
    {
        // Act
        var result = await _sut.GenerateUploadUrlAsync("uploads/probe/file.png", "image/png", 1024, TimeSpan.FromMinutes(5));

        // Assert
        result.Url.Scheme.ShouldBe("local");
        result.RequiredHeaders["Content-Type"].ShouldBe("image/png");
        result.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GenerateDownloadUrlAsync_Should_ReturnRelativeUrl_When_KeyProvided()
    {
        // Act
        var uri = await _sut.GenerateDownloadUrlAsync("/uploads/probe/file.png", TimeSpan.FromMinutes(5));

        // Assert
        uri.IsAbsoluteUri.ShouldBeFalse();
        uri.OriginalString.ShouldBe("/uploads/probe/file.png");
    }

    [Fact]
    public void BuildPublicUrl_Should_NormalizeToServerRelativePath_When_KeyHasBackslashes()
    {
        // Act
        var url = _sut.BuildPublicUrl("uploads\\probe\\file.png");

        // Assert
        url.ShouldBe("/uploads/probe/file.png");
    }

    #endregion

    #region Exception / Edge Cases

    [Fact]
    public async Task UploadAsync_Should_Throw_When_ExtensionNotAllowed()
    {
        // Arrange
        var request = PngRequest("malware.exe");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.UploadAsync<Probe>(request, FileType.Image));
    }

    [Fact]
    public async Task UploadAsync_Should_Throw_When_FileExceedsMaxSize()
    {
        // Arrange — Image limit is 5 MB; build a 6 MB payload.
        var request = new FileUploadRequest
        {
            FileName = "big.png",
            ContentType = "image/png",
            Data = new List<byte>(new byte[6 * 1024 * 1024])
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.UploadAsync<Probe>(request, FileType.Image));
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnFalse_When_PathBlank()
    {
        (await _sut.ExistsAsync(" ")).ShouldBeFalse();
        (await _sut.GetSizeAsync(" ")).ShouldBe(0);
        (await _sut.DownloadAsync(" ")).ShouldBeNull();
        (await _sut.HeadObjectAsync(" ")).ShouldBeNull();
    }

    [Fact]
    public async Task DownloadAsync_Should_ReturnNull_When_FileMissing()
    {
        (await _sut.DownloadAsync("uploads/probe/missing.png")).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAsync_Should_NotThrow_When_PathBlankOrMissing()
    {
        await Should.NotThrowAsync(() => _sut.RemoveAsync(string.Empty));
        await Should.NotThrowAsync(() => _sut.RemoveAsync("uploads/probe/missing.png"));
    }

    #endregion

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort temp cleanup
        }
    }
}
