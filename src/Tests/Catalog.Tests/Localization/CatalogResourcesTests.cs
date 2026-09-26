using System.Globalization;
using System.Linq;
using FSH.Modules.Catalog.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Catalog.Tests.Localization;

// Proves the CatalogResources catalog is embedded under the correct manifest name (ResourcesPath="" =>
// co-located marker + resx). A wrong manifest name flips ResourceNotFound and leaks raw keys; a
// missing pt-BR entry ships English as "translated". Both are caught here.
public sealed class CatalogResourcesTests
{
    private static IStringLocalizer BuildLocalizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider()
            .GetRequiredService<IStringLocalizerFactory>()
            .Create(typeof(CatalogResources));
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
        var neutral = KeysFor(string.Empty);   // CatalogResources.resx (English / fallback)
        var pt = KeysFor("pt-BR");                 // CatalogResources.pt-BR.resx

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
            var en = localizer["Catalog.CategoryCannotBeOwnParent"];
            en.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Catalog.CategoryCannotBeOwnParent' for en-US — check ResourcesPath/resx manifest name.");
            en.Value.ShouldBe("A category cannot be its own parent.");

            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            var pt = localizer["Catalog.CategoryCannotBeOwnParent"];
            pt.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Catalog.CategoryCannotBeOwnParent' for pt-BR — check the .pt-BR catalog manifest name.");
            pt.Value.ShouldBe("Uma categoria não pode ser pai de si mesma.");

            pt.Value.ShouldNotBe(en.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // The AdjustStock domain overflow message is localized with two positional args ({0}=delta, {1}=current stock).
    [Fact]
    public void StockAdjustmentNegative_formats_args_in_both_cultures()
    {
        var localizer = BuildLocalizer();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var en = localizer["Catalog.StockAdjustmentNegative", -4, 3];
            en.ResourceNotFound.ShouldBeFalse();
            en.Value.ShouldBe("Stock adjustment of -4 would result in negative stock (current: 3).");

            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            var pt = localizer["Catalog.StockAdjustmentNegative", -4, 3];
            pt.ResourceNotFound.ShouldBeFalse();
            pt.Value.ShouldBe("O ajuste de estoque de -4 resultaria em estoque negativo (atual: 3).");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
