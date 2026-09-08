using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Services;

namespace Identity.Tests.Services;

/// <summary>
/// The JWT carries the OIDC-standard `locale` claim only when the user explicitly chose a language;
/// an unset locale emits no claim so culture resolution can fall through to Accept-Language.
/// </summary>
public sealed class CreateBasicClaimsTests
{
    private static FshUser User(string? locale) =>
        new() { Id = "u1", Email = "u@codefi.com.br", UserName = "u", FirstName = "First", LastName = "Last", Locale = locale };

    [Fact]
    public void Emits_locale_claim_when_user_locale_is_set()
    {
        var claims = IdentityService.CreateBasicClaims(User("pt-BR"), "codefi");

        claims.Single(c => c.Type == "locale").Value.ShouldBe("pt-BR");
    }

    [Fact]
    public void Omits_locale_claim_when_user_locale_is_null()
    {
        var claims = IdentityService.CreateBasicClaims(User(null), "codefi");

        claims.Any(c => c.Type == "locale").ShouldBeFalse();
    }

    [Fact]
    public void Omits_locale_claim_when_user_locale_is_whitespace()
    {
        var claims = IdentityService.CreateBasicClaims(User("   "), "codefi");

        claims.Any(c => c.Type == "locale").ShouldBeFalse();
    }
}
