using System.ComponentModel.DataAnnotations;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity;
using FSH.Modules.Identity.Contracts.v1.TwoFactor;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Features.v1.TwoFactor.Enroll;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Identity.Tests.Handlers;

/// <summary>
/// Tests for EnrollTwoFactorCommandHandler - the otpauth URI an authenticator app scans.
/// </summary>
public sealed class EnrollTwoFactorCommandHandlerTests
{
    private const string SharedKey = "JBSWY3DPEHPK3PXP";
    private static readonly Guid UserId = Guid.Parse("7f1c2a4e-3b5d-4c6e-8f9a-0b1c2d3e4f50");

    private readonly UserManager<FshUser> _userManager;
    private readonly ICurrentUser _currentUser;

    public EnrollTwoFactorCommandHandlerTests()
    {
        _userManager = Substitute.For<UserManager<FshUser>>(
            Substitute.For<IUserStore<FshUser>>(), null, null, null, null, null, null, null, null);
        _currentUser = Substitute.For<ICurrentUser>();

        var user = new FshUser { Id = UserId.ToString(), Email = "jane@example.com" };
        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(UserId);
        _userManager.FindByIdAsync(UserId.ToString()).Returns(user);
        _userManager.ResetAuthenticatorKeyAsync(user).Returns(IdentityResult.Success);
        _userManager.GetAuthenticatorKeyAsync(user).Returns(SharedKey);
    }

    private EnrollTwoFactorCommandHandler CreateHandler(TwoFactorOptions options) =>
        new(_userManager, _currentUser, Options.Create(options));

    [Fact]
    public async Task Handle_Should_UseDefaultIssuer_When_IssuerIsNotConfigured()
    {
        // The template rewrites the brand in scaffolded projects (possibly to a name with spaces),
        // so the expectation is derived from the default instead of spelling it out.
        var issuer = Uri.EscapeDataString(new TwoFactorOptions().Issuer);
        var sut = CreateHandler(new TwoFactorOptions());

        var result = await sut.Handle(new EnrollTwoFactorCommand(), CancellationToken.None);

        result.AuthenticatorUri.ShouldBe(
            $"otpauth://totp/{issuer}:jane@example.com?secret={SharedKey}&issuer={issuer}&digits=6");
    }

    [Fact]
    public void TwoFactorOptions_Should_DefaultIssuerToTheBrand()
    {
        new TwoFactorOptions().Issuer.ShouldBe("FullStackHero");
    }

    [Fact]
    public async Task Handle_Should_UseConfiguredIssuer_InLabelAndIssuerParameter()
    {
        var sut = CreateHandler(new TwoFactorOptions { Issuer = "Acme" });

        var result = await sut.Handle(new EnrollTwoFactorCommand(), CancellationToken.None);

        result.AuthenticatorUri.ShouldBe(
            $"otpauth://totp/Acme:jane@example.com?secret={SharedKey}&issuer=Acme&digits=6");
    }

    [Theory]
    [InlineData("Canal Correto", "Canal%20Correto")]
    [InlineData("Ação", "A%C3%A7%C3%A3o")]
    [InlineData("A:B & C", "A%3AB%20%26%20C")]
    public async Task Handle_Should_PercentEncodeIssuer_When_ItHasSpacesAccentsOrReservedCharacters(
        string issuer, string encoded)
    {
        var sut = CreateHandler(new TwoFactorOptions { Issuer = issuer });

        var result = await sut.Handle(new EnrollTwoFactorCommand(), CancellationToken.None);

        result.AuthenticatorUri.ShouldBe(
            $"otpauth://totp/{encoded}:jane@example.com?secret={SharedKey}&issuer={encoded}&digits=6");
        Uri.TryCreate(result.AuthenticatorUri, UriKind.Absolute, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_UserIsNotAuthenticated()
    {
        _currentUser.IsAuthenticated().Returns(false);
        var sut = CreateHandler(new TwoFactorOptions());

        await Should.ThrowAsync<UnauthorizedException>(
            async () => await sut.Handle(new EnrollTwoFactorCommand(), CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TwoFactorOptions_Should_RejectBlankIssuer(string issuer)
    {
        var options = new TwoFactorOptions { Issuer = issuer };

        var isValid = Validator.TryValidateObject(
            options, new ValidationContext(options), new List<ValidationResult>(), validateAllProperties: true);

        isValid.ShouldBeFalse();
    }
}
