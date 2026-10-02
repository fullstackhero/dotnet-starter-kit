using System.Globalization;
using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Localization;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// The stale-profile 412 is shown to the user by the dashboard, so it must travel with a MessageKey
/// the global handler can localize, and that key must exist in both catalogs.
/// </summary>
public sealed class StaleProfileLocalizationTests
{
    [Fact]
    public async Task UpdateAsync_with_a_stale_stamp_throws_a_localizable_412()
    {
        // Arrange
        var userManager = Substitute.For<UserManager<FshUser>>(
            Substitute.For<IUserStore<FshUser>>(), null, null, null, null, null, null, null, null);
        var signInManager = Substitute.For<SignInManager<FshUser>>(
            userManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<FshUser>>(),
            Options.Create(new IdentityOptions()),
            Substitute.For<ILogger<SignInManager<FshUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<FshUser>>());
        var user = new FshUser { Id = "u1", Email = "u@codefi.com.br", UserName = "u", ConcurrencyStamp = "current" };
        userManager.FindByIdAsync("u1").Returns(user);
        var sut = new UserProfileService(
            userManager,
            signInManager,
            Substitute.For<IStorageService>(),
            Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>(),
            Substitute.For<IRequestContextService>(),
            new IdentityErrorDescriber());

        // Act
        var ex = await Should.ThrowAsync<CustomException>(() =>
            sut.UpdateAsync("u1", "First", "Last", string.Empty, null!, false, null, ["stale"], CancellationToken.None));

        // Assert
        ex.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        ex.MessageKey.ShouldBe("Identity.ProfileChangedSinceLoaded");
        ex.ResourceSource.ShouldBe(typeof(IdentityResources));
    }

    [Theory]
    [InlineData("en-US", "The profile changed since you loaded it. Reload it and apply your changes again.")]
    [InlineData("pt-BR", "O perfil mudou desde que você o carregou. Recarregue e aplique suas alterações de novo.")]
    public void The_stale_profile_key_resolves_in_every_catalog(string culture, string expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<IdentityResources>>();

        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            var message = localizer["Identity.ProfileChangedSinceLoaded"];

            message.ResourceNotFound.ShouldBeFalse();
            message.Value.ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
