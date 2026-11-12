using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Local;
using Microsoft.AspNetCore.Hosting;

namespace Framework.Tests.Storage;

/// <summary>
/// Keys reach the storage layer from stored values that a client can influence (an avatar URL, a logo
/// URL). These pin that the local provider never reads, deletes or copies outside its own roots, and that
/// the helpers modules use to decide what they may delete reject anything they did not create.
/// </summary>
public sealed class StoragePathSafetyTests : IDisposable
{
    private sealed class Probe { }

    private sealed class Other { }

    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private readonly string _site;
    private readonly string _webRoot;
    private readonly string _secret;
    private readonly string _siteAsset;
    private readonly LocalStorageService _sut;

    public StoragePathSafetyTests()
    {
        // site/                 <- content root (web.config, appsettings live here on IIS)
        //   appsettings.json    <- must never be reachable
        //   wwwroot/            <- web root
        //     index.html        <- a static asset that is not a storage object
        _site = Path.Combine(Path.GetTempPath(), "fsh-storage-safety", Guid.NewGuid().ToString("N"));
        _webRoot = Path.Combine(_site, "wwwroot");
        Directory.CreateDirectory(_webRoot);
        _secret = Path.Combine(_site, "appsettings.json");
        File.WriteAllText(_secret, "{\"secret\":true}");
        _siteAsset = Path.Combine(_webRoot, "index.html");
        File.WriteAllText(_siteAsset, "<html></html>");

        var environment = Substitute.For<IWebHostEnvironment>();
        environment.WebRootPath.Returns(_webRoot);
        environment.ContentRootPath.Returns(_site);
        _sut = new LocalStorageService(environment);
    }

    public void Dispose()
    {
        if (Directory.Exists(_site))
        {
            Directory.Delete(_site, recursive: true);
        }
    }

    public static TheoryData<string> HostileKeys() => new()
    {
        "../appsettings.json",
        "uploads/../../appsettings.json",
        "uploads\\..\\..\\appsettings.json",
        "/../appsettings.json",
        "index.html",                         // inside the web root, but not a storage object
        "uploads/probe/x.png:hidden",         // NTFS alternate data stream
        "uploads/probe/x.png.",               // Windows strips the trailing dot
        "file:///etc/passwd",
        "C:\\Windows\\win.ini",
    };

    [Theory]
    [MemberData(nameof(HostileKeys))]
    public async Task RemoveAsync_Should_LeaveFilesOutsideTheStorageRootsAlone(string key)
    {
        await _sut.RemoveAsync(key);

        File.Exists(_secret).ShouldBeTrue();
        File.Exists(_siteAsset).ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAsync_Should_IgnoreAnAbsolutePath()
    {
        // Path.Combine(root, rooted) returns the rooted path, which is how an absolute path deleted files.
        await _sut.RemoveAsync(_secret);

        File.Exists(_secret).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(HostileKeys))]
    public async Task ReadOperations_Should_NotResolveOutsideTheStorageRoots(string key)
    {
        (await _sut.DownloadAsync(key)).ShouldBeNull();
        (await _sut.ExistsAsync(key)).ShouldBeFalse();
        (await _sut.GetSizeAsync(key)).ShouldBe(0);
        (await _sut.HeadObjectAsync(key)).ShouldBeNull();
    }

    [Fact]
    public async Task CopyAsync_Should_Refuse_WhenEitherKeyLeavesTheStorageRoots()
    {
        var stored = await _sut.UploadAsync<Probe>(new FileUploadRequest { FileName = "a.png", ContentType = "image/png", Data = [.. PngBytes] }, FileType.Image);

        await Should.ThrowAsync<ArgumentException>(() => _sut.CopyAsync(stored, "../appsettings.json"));
        await Should.ThrowAsync<ArgumentException>(() => _sut.CopyAsync("../appsettings.json", "public/stolen.json"));
        (await File.ReadAllTextAsync(_secret)).ShouldBe("{\"secret\":true}");
    }

    [Fact]
    public async Task UploadAsync_Should_Reject_ContentThatIsNotTheDeclaredImageType()
    {
        var request = new FileUploadRequest { FileName = "avatar.png", ContentType = "image/png", Data = [.. "<html><script>alert(1)</script></html>"u8] };

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.UploadAsync<Probe>(request, FileType.Image));
    }

    [Fact]
    public async Task UploadAsync_Should_KeepAHostileFileNameInsideTheUploadFolder()
    {
        var request = new FileUploadRequest { FileName = "..\\..\\..\\web.config.png", ContentType = "image/png", Data = [.. PngBytes] };

        var stored = await _sut.UploadAsync<Probe>(request, FileType.Image);

        // The name keeps its dots but loses every separator, so it is one harmless segment in the folder.
        stored.ShouldStartWith("uploads/probe/");
        stored.Split('/').Length.ShouldBe(3);
        stored.Split('/').ShouldNotContain("..");
        StorageUploadKeys.IsUploadOf<Probe>(stored).ShouldBeTrue();
    }

    [Theory]
    [InlineData("uploads/probe/0123456789abcdef0123456789abcdef_avatar.png", true)]
    [InlineData("/uploads/probe/0123456789abcdef0123456789abcdef_avatar.png", true)]
    [InlineData("public/uploads/probe/0123456789abcdef0123456789abcdef_avatar.png", true)]
    [InlineData("https://bucket.s3.amazonaws.com/prefix/public/uploads/probe/0123456789abcdef0123456789abcdef_a.png", true)]
    [InlineData("uploads/other/0123456789abcdef0123456789abcdef_avatar.png", false)]
    [InlineData("uploads/probe/../../appsettings.json", false)]
    [InlineData("uploads/probe/avatar.png", false)]
    [InlineData("uploads\\probe\\0123456789abcdef0123456789abcdef_avatar.png", false)]
    [InlineData("C:/site/uploads/probe/0123456789abcdef0123456789abcdef_avatar.png", false)]
    [InlineData("file:///uploads/probe/0123456789abcdef0123456789abcdef_avatar.png", false)]
    [InlineData("https://cdn.example.com/avatar.png", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsUploadOf_Should_RecognizeOnlyObjectsUploadAsyncCreated(string? value, bool expected)
    {
        StorageUploadKeys.IsUploadOf<Probe>(value).ShouldBe(expected);
    }

    [Fact]
    public void IsUploadOf_Should_DistinguishUploadTypes()
    {
        var key = StorageUploadKeys.NewKey<Other>("x.png");

        StorageUploadKeys.IsUploadOf<Other>(key).ShouldBeTrue();
        StorageUploadKeys.IsUploadOf<Probe>(key).ShouldBeFalse();
    }

    [Theory]
    [InlineData("https://cdn.example.com/logo.png", true)]
    [InlineData("http://localhost:7030/public/tenants/root/logo.png", true)]
    [InlineData("/public/tenants/root/user/2026/11/abc/logo.png", true)]
    [InlineData("../../web.config", false)]
    [InlineData("/uploads/../../web.config", false)]
    [InlineData("C:\\inetpub\\wwwroot\\web.config", false)]
    [InlineData("\\\\attacker\\share\\x.png", false)]
    [InlineData("//attacker.example/x.png", false)]
    [InlineData("file:///C:/Windows/win.ini", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:image/png;base64,AAAA", false)]
    [InlineData("uploads/fshuser/x.png", false)]
    public void ClientImageUrl_Should_AcceptOnlyHttpUrlsAndSafeServerPaths(string value, bool expected)
    {
        ClientImageUrl.IsValid(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, true)]
    [InlineData(".JPG", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)]
    [InlineData(".jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xDB }, true)]
    [InlineData(".ico", new byte[] { 0x00, 0x00, 0x01, 0x00, 0x01 }, true)]
    [InlineData(".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 }, true)]
    [InlineData(".png", new byte[] { 0xFF, 0xD8, 0xFF }, false)]
    [InlineData(".png", new byte[] { 0x89, 0x50 }, false)]
    [InlineData(".svg", new byte[] { 0x3C, 0x73, 0x76, 0x67 }, false)]
    public void FileSignatures_Should_MatchOnlyTheExtensionsRealSignature(string extension, byte[] data, bool expected)
    {
        FileSignatures.Matches(extension, data).ShouldBe(expected);
    }
}
