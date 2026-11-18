using System.Security.Claims;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Localization;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// When an external sign-in brings a user name that is already taken, GetOrCreateFromPrincipalAsync
/// derives a new one by appending a random suffix and capping the result at 20 characters. The cap
/// must not cut the random part away, or the "unique" name is the taken name (or a fixed prefix of it).
/// </summary>
public sealed class ExternalUserNameUniquenessTests
{
    private const int MinimumRandomHexChars = 8;

    private readonly UserManager<FshUser> _userManager = Substitute.For<UserManager<FshUser>>(
        Substitute.For<IUserStore<FshUser>>(), null, null, null, null, null, null, null, null);

    private readonly List<string> _attemptedUserNames = [];

    public ExternalUserNameUniquenessTests()
    {
        // Record the name the service tries to create, then stop the flow there: role and group
        // assignment need a DbContext and are not what this test is about.
        _userManager.CreateAsync(Arg.Do<FshUser>(u => _attemptedUserNames.Add(u.UserName!)))
            .Returns(IdentityResult.Failed(new IdentityError { Code = "StopAfterCreate" }));
    }

    private UserRegistrationService CreateSut()
    {
        var tenant = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        tenant.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("root", "root")));

        return new UserRegistrationService(
            _userManager,
            db: null!,
            Substitute.For<IJobService>(),
            Substitute.For<IMailService>(),
            tenant,
            Substitute.For<IOutboxStore>(),
            Substitute.For<IStringLocalizer<IdentityResources>>());
    }

    private async Task<string> AttemptExternalSignInAsync(string takenUserName)
    {
        _userManager.FindByNameAsync(takenUserName).Returns(new FshUser { UserName = takenUserName });

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "new.person@example.com"), new Claim(ClaimTypes.Name, takenUserName)],
            authenticationType: "external"));

        await Should.ThrowAsync<CustomException>(() => CreateSut().GetOrCreateFromPrincipalAsync(principal));

        return _attemptedUserNames[^1];
    }

    [Theory]
    [InlineData("bob")]                          // short: plenty of room for the suffix
    [InlineData("abcdefghijklmnopqr")]           // 18 chars: one hex char survived the cap
    [InlineData("abcdefghijklmnopqrs")]          // 19 chars: only the '_' survived
    [InlineData("christopher.anderson")]         // 20 chars: the taken name came back unchanged
    [InlineData("christopher.anderson.junior")]  // over 20: a fixed prefix of the taken name
    public async Task GetOrCreateFromPrincipalAsync_Should_KeepRandomSuffix_When_UserNameIsTaken(string takenUserName)
    {
        var attempted = await AttemptExternalSignInAsync(takenUserName);

        attempted.ShouldNotBe(takenUserName);
        attempted.Length.ShouldBeLessThanOrEqualTo(20);
        var suffix = attempted[(attempted.LastIndexOf('_') + 1)..];
        suffix.Length.ShouldBeGreaterThanOrEqualTo(MinimumRandomHexChars);
        suffix.ShouldAllBe(c => Uri.IsHexDigit(c));
    }

    [Fact]
    public async Task GetOrCreateFromPrincipalAsync_Should_DeriveDifferentNames_When_SameLongUserNameCollidesTwice()
    {
        var first = await AttemptExternalSignInAsync("christopher.anderson");
        var second = await AttemptExternalSignInAsync("christopher.anderson");

        first.ShouldNotBe(second);
    }
}
