using System.Globalization;
using System.Linq;

namespace Framework.Tests.Localization;

// Guards against an English key missing from the .pt-BR catalog (a silent English fallback shipped as
// "translated"). Enumerates each culture's own embedded resx (includeParentCultures: false) and
// asserts identical key sets.
public sealed class SharedResourcesKeyParityTests
{
    private static List<string> KeysFor(string culture)
    {
        var localizer = SharedResourcesLocalizerFactory.Create();
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
        var neutral = KeysFor(string.Empty);   // SharedResources.resx (English / fallback)
        var pt = KeysFor("pt-BR");                 // SharedResources.pt-BR.resx

        neutral.ShouldNotBeEmpty();
        pt.OrderBy(k => k, StringComparer.Ordinal)
            .ShouldBe(neutral.OrderBy(k => k, StringComparer.Ordinal));
    }
}
