using FSH.Framework.Shared.Storage;
using FSH.Modules.Identity.Contracts.v1.Users.SetProfileImage;
using FSH.Modules.Identity.Features.v1.Users;
using FSH.Modules.Identity.Features.v1.Users.SetProfileImage;
using Identity.Tests.Support;
using Shouldly;
using Xunit;

namespace Identity.Tests.Validators;

public sealed class ProfileImageValidatorTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://cdn.example.com/me.png", true)]
    [InlineData("/public/tenants/root/user/2026/11/abc/me.png", true)]
    [InlineData("../../web.config", false)]
    [InlineData("..\\..\\appsettings.json", false)]
    [InlineData("C:\\inetpub\\wwwroot\\web.config", false)]
    [InlineData("\\\\attacker\\share\\x.png", false)]
    [InlineData("file:///C:/Windows/win.ini", false)]
    [InlineData("javascript:alert(1)", false)]
    public void SetProfileImage_Should_AcceptOnlyHttpUrlsAndSafeServerPaths(string? imageUrl, bool expectedValid)
    {
        var sut = new SetProfileImageCommandValidator(SharedResourcesLocalizerFactory.Create());

        sut.Validate(new SetProfileImageCommand(imageUrl)).IsValid.ShouldBe(expectedValid);
    }

    [Fact]
    public void UserImage_Should_Reject_BytesThatAreNotThePromisedImage()
    {
        var sut = new UserImageValidator(SharedResourcesLocalizerFactory.Create());
        var html = new FileUploadRequest { FileName = "avatar.png", ContentType = "image/png", Data = [.. "<html><script>alert(1)</script></html>"u8] };
        var png = new FileUploadRequest { FileName = "avatar.png", ContentType = "image/png", Data = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1] };

        sut.Validate(html).IsValid.ShouldBeFalse();
        sut.Validate(png).IsValid.ShouldBeTrue();
    }
}
