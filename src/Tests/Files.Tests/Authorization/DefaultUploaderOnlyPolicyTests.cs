using System.Security.Claims;
using FSH.Modules.Files.Authorization;
using FSH.Modules.Files.Contracts;

namespace Files.Tests.Authorization;

public class DefaultUploaderOnlyPolicyTests
{
    private static ClaimsPrincipal User(string userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));

    private static FileAccessContext PublicFileOwnedBy(string uploaderId) =>
        new(Guid.NewGuid(), "MyFiles", null, uploaderId, Visibility: 0);

    private static FileAccessContext PrivateFileOwnedBy(string uploaderId) =>
        new(Guid.NewGuid(), "MyFiles", null, uploaderId, Visibility: 1);

    [Fact]
    public async Task CanAttachAsync_Should_AllowAuthenticated()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanAttachAsync(null, User("u1"), default)).ShouldBeTrue();
    }

    [Fact]
    public async Task CanAttachAsync_Should_DenyAnonymous()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        (await p.CanAttachAsync(null, anonymous, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task CanReadAsync_Should_AllowAnyone_ForPublicFile()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanReadAsync(PublicFileOwnedBy("uploader"), User("someone-else"), default)).ShouldBeTrue();
    }

    [Fact]
    public async Task CanReadAsync_Should_AllowUploaderOnly_ForPrivateFile()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanReadAsync(PrivateFileOwnedBy("uploader"), User("uploader"), default)).ShouldBeTrue();
        (await p.CanReadAsync(PrivateFileOwnedBy("uploader"), User("someone-else"), default)).ShouldBeFalse();
    }

    [Fact]
    public async Task CanDeleteAsync_Should_AllowUploaderOnly()
    {
        var p = new DefaultUploaderOnlyPolicy("MyFiles");
        (await p.CanDeleteAsync(PublicFileOwnedBy("uploader"), User("uploader"), default)).ShouldBeTrue();
        (await p.CanDeleteAsync(PublicFileOwnedBy("uploader"), User("someone-else"), default)).ShouldBeFalse();
    }
}
