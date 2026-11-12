using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// The avatar field holds either an upload the profile service made or a URL the client set through
/// SetImageUrlAsync. "Delete current image" used to hand whatever was stored to the storage layer, so
/// a user could store <c>../../web.config</c> (or another user's avatar key) and then have it deleted.
/// </summary>
public sealed class ProfileImageStorageSafetyTests
{
    private const string OwnUpload = "uploads/fshuser/0123456789abcdef0123456789abcdef_me.png";

    private readonly UserManager<FshUser> _userManager;
    private readonly SignInManager<FshUser> _signInManager;
    private readonly IStorageService _storageService;

    public ProfileImageStorageSafetyTests()
    {
        _userManager = Substitute.For<UserManager<FshUser>>(
            Substitute.For<IUserStore<FshUser>>(), null, null, null, null, null, null, null, null);
        _signInManager = Substitute.For<SignInManager<FshUser>>(
            _userManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<FshUser>>(),
            Options.Create(new IdentityOptions()),
            Substitute.For<ILogger<SignInManager<FshUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<FshUser>>());
        _signInManager.RefreshSignInAsync(Arg.Any<FshUser>()).Returns(Task.CompletedTask);
        _storageService = Substitute.For<IStorageService>();
    }

    private UserProfileService CreateSut()
    {
        var tenant = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        tenant.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("root", "root")));
        return new(_userManager, _signInManager, _storageService, tenant,
            Substitute.For<IRequestContextService>(), new IdentityErrorDescriber());
    }

    private FshUser UserWithImage(string? imageUrl)
    {
        var user = new FshUser
        {
            Id = "u1",
            Email = "u@example.com",
            UserName = "u",
            ImageUrl = imageUrl is null ? null : new Uri(imageUrl, UriKind.RelativeOrAbsolute),
        };
        _userManager.FindByIdAsync("u1").Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        return user;
    }

    [Theory]
    [InlineData("../../web.config")]
    [InlineData("..\\..\\appsettings.json")]
    [InlineData("https://bucket.s3.amazonaws.com/public/tenants/other/user/2026/11/0123/secret.pdf")]
    [InlineData("https://cdn.example.com/avatars/me.png")]
    public async Task UpdateAsync_Should_NotDeleteTheStoredValue_When_ItIsNotAnAvatarThisServiceUploaded(string storedImageUrl)
    {
        UserWithImage(storedImageUrl);

        await CreateSut().UpdateAsync("u1", "First", "Last", string.Empty, null!, deleteCurrentImage: true, null, null, CancellationToken.None);

        await _storageService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_Should_DeleteTheAvatar_When_ThisServiceUploadedIt()
    {
        var user = UserWithImage(OwnUpload);

        await CreateSut().UpdateAsync("u1", "First", "Last", string.Empty, null!, deleteCurrentImage: true, null, null, CancellationToken.None);

        await _storageService.Received(1).RemoveAsync(OwnUpload, Arg.Any<CancellationToken>());
        user.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task SetImageUrlAsync_Should_Reject_AnotherStoredUpload()
    {
        UserWithImage(OwnUpload);
        const string victimsAvatar = "/uploads/fshuser/fedcba9876543210fedcba9876543210_victim.png";

        var ex = await Should.ThrowAsync<CustomException>(
            () => CreateSut().SetImageUrlAsync("u1", victimsAvatar, CancellationToken.None));

        ex.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<FshUser>());
    }

    [Fact]
    public async Task SetImageUrlAsync_Should_Accept_AnExternalUrl_And_TheCurrentAvatar()
    {
        var user = UserWithImage(OwnUpload);

        await CreateSut().SetImageUrlAsync("u1", OwnUpload, CancellationToken.None);
        await CreateSut().SetImageUrlAsync("u1", "https://cdn.example.com/avatars/me.png", CancellationToken.None);

        user.ImageUrl!.OriginalString.ShouldBe("https://cdn.example.com/avatars/me.png");
    }
}
