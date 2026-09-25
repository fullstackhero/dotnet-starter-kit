using System.Globalization;
using System.Text;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Localization;
using FSH.Modules.Identity.Services;
using Identity.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// The confirm-email success text is rendered verbatim by both SPAs' confirmation page, so it has to
/// follow the caller's language like every other server message.
/// </summary>
public sealed class ConfirmEmailMessageTests
{
    [Theory]
    [InlineData("en-US", "Your email u@codefi.com.br is confirmed. You can now sign in.")]
    [InlineData("pt-BR", "Seu e-mail u@codefi.com.br foi confirmado. Agora você já pode entrar.")]
    public async Task ConfirmEmailAsync_returns_the_success_message_in_the_request_language(string culture, string expected)
    {
        // Arrange
        var user = new FshUser { Id = "u1", Email = "u@codefi.com.br", UserName = "u", EmailConfirmed = false };
        var userManager = Substitute.For<UserManager<FshUser>>(
            Substitute.For<IUserStore<FshUser>>(), null, null, null, null, null, null, null, null);
        userManager.Users.Returns(new[] { user }.AsAsyncQueryable());
        userManager.ConfirmEmailAsync(user, "token").Returns(IdentityResult.Success);

        var tenant = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        tenant.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("root", "root")));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<IdentityResources>>();

        var sut = new UserRegistrationService(
            userManager,
            db: null!,
            Substitute.For<IJobService>(),
            Substitute.For<IMailService>(),
            tenant,
            Substitute.For<IOutboxStore>(),
            localizer);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token"));

        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            // Act
            var message = await sut.ConfirmEmailAsync("u1", code, "root", CancellationToken.None);

            // Assert
            message.ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
