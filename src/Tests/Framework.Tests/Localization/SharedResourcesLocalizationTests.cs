using System.Globalization;
using FSH.Framework.Core.Localization;
using FSH.Framework.Web.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Framework.Tests.Localization;

// Proves the whole resx wiring: SharedResources marker + co-located resx + ResourcesPath="" in
// AddHeroLocalization resolve to the embedded catalog under the current UI culture. If the manifest
// name or ResourcesPath is wrong, ResourceNotFound flips true and IStringLocalizer leaks the raw key.
public sealed class SharedResourcesLocalizationTests
{
    private static IStringLocalizer<SharedResources> BuildLocalizer()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHeroLocalization(configuration);
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResources>>();
    }

    // Catalogs are named for specific cultures (SharedResources.pt-BR.resx), matching the front-end.
    // The consequence is deliberate and pinned here: only pt-BR is served Portuguese. A bare `pt` or
    // an unsupported variant like pt-PT walks its parent chain, finds no catalog of its own and lands
    // on the neutral (English) one, rather than being silently handed Brazilian strings.
    [Theory]
    [InlineData("pt-BR", "Não encontrado")]   // specific pt-BR resolves directly
    [InlineData("pt", "Not Found")]           // bare pt has no catalog -> neutral English
    [InlineData("pt-PT", "Not Found")]         // unsupported variant -> neutral English, NOT pt-BR
    [InlineData("en-US", "Not Found")]        // en-US falls back to the neutral (default) catalog
    public void Localizer_resolves_error_key_per_culture(string culture, string expected)
    {
        var localizer = BuildLocalizer();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var localized = localizer["Error.NotFound"];

            localized.ResourceNotFound.ShouldBeFalse(
                $"resx for '{culture}' did not resolve 'Error.NotFound' — check ResourcesPath/resx manifest name.");
            localized.Value.ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
