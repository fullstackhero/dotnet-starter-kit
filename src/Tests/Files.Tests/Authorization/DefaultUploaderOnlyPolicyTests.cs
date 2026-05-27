using FSH.Modules.Files.Authorization;
using FSH.Modules.Files.Contracts;

namespace Files.Tests.Authorization;

public class DefaultUploaderOnlyPolicyTests
{
    private static FileAccessContext PublicFileOwnedBy(string uploaderId) =>
        new(Guid.NewGuid(), "MyFiles", null, uploaderId, Visibility: 0);

    private static FileAccessContext PrivateFileOwnedBy(string uploaderId) =>
        new(Guid.NewGuid(), "MyFiles", null, uploaderId, Visibility: 1);

    [Fact]
    public async Task CanAttachAsync_Should_AllowAuthenticated()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanAttachAsync(null, "user-1", default)).ShouldBeTrue();
    }

    [Fact]
    public async Task CanAttachAsync_Should_DenyAnonymous()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanAttachAsync(null, "", default)).ShouldBeFalse();
    }

    [Fact]
    public async Task CanReadAsync_Should_AllowAnyone_ForPublicFile()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanReadAsync(PublicFileOwnedBy("uploader"), "someone-else", default)).ShouldBeTrue();
    }

    [Fact]
    public async Task CanReadAsync_Should_AllowUploaderOnly_ForPrivateFile()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanReadAsync(PrivateFileOwnedBy("uploader"), "uploader", default)).ShouldBeTrue();
        (await p.CanReadAsync(PrivateFileOwnedBy("uploader"), "someone-else", default)).ShouldBeFalse();
    }

    [Fact]
    public async Task CanDeleteAsync_Should_AllowUploaderOnly()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanDeleteAsync(PublicFileOwnedBy("uploader"), "uploader", default)).ShouldBeTrue();
        (await p.CanDeleteAsync(PublicFileOwnedBy("uploader"), "someone-else", default)).ShouldBeFalse();
    }
}
