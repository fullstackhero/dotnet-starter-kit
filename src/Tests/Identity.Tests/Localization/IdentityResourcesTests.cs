using System.Globalization;
using System.Linq;
using FSH.Modules.Identity.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Identity.Tests.Localization;

// Proves the IdentityResources catalog is embedded under the correct manifest name (ResourcesPath="" =>
// co-located marker + resx). A wrong manifest name flips ResourceNotFound and leaks raw keys; a
// missing pt-BR entry ships English as "translated". Both are caught here.
public sealed class IdentityResourcesTests
{
    private static IStringLocalizer BuildLocalizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider()
            .GetRequiredService<IStringLocalizerFactory>()
            .Create(typeof(IdentityResources));
    }

    private static List<string> KeysFor(string culture)
    {
        var localizer = BuildLocalizer();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = culture.Length == 0
                ? CultureInfo.InvariantCulture
                : new CultureInfo(culture);
            return localizer.GetAllStrings(includeParentCultures: false)
                .Select(s => s.Name)
                .ToList();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Neutral_and_ptBR_catalogs_have_matching_keys()
    {
        var neutral = KeysFor(string.Empty);   // IdentityResources.resx (English / fallback)
        var pt = KeysFor("pt-BR");                 // IdentityResources.pt-BR.resx

        neutral.ShouldNotBeEmpty();
        pt.OrderBy(k => k, StringComparer.Ordinal)
            .ShouldBe(neutral.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void Known_key_resolves_and_differs_between_en_and_pt()
    {
        var localizer = BuildLocalizer();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var en = localizer["Identity.UserNotFound"];
            en.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Identity.UserNotFound' for en-US — check ResourcesPath/resx manifest name.");
            en.Value.ShouldBe("User not found.");

            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            var pt = localizer["Identity.UserNotFound"];
            pt.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Identity.UserNotFound' for pt-BR — check the .pt-BR catalog manifest name.");
            pt.Value.ShouldBe("Usuário não encontrado.");

            pt.Value.ShouldNotBe(en.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
