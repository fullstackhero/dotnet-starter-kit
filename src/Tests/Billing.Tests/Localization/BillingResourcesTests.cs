using System.Globalization;
using System.Linq;
using FSH.Modules.Billing.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Billing.Tests.Localization;

// Proves the BillingResources catalog is embedded under the correct manifest name (ResourcesPath="" =>
// co-located marker + resx). A wrong manifest name flips ResourceNotFound and leaks raw keys; a
// missing pt-BR entry ships English as "translated". Both are caught here.
public sealed class BillingResourcesTests
{
    private static IStringLocalizer BuildLocalizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider()
            .GetRequiredService<IStringLocalizerFactory>()
            .Create(typeof(BillingResources));
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
        var neutral = KeysFor(string.Empty);   // BillingResources.resx (English / fallback)
        var pt = KeysFor("pt-BR");                 // BillingResources.pt-BR.resx

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
            var en = localizer["Billing.OnlyRootOperatorMayGenerateInvoices"];
            en.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Billing.OnlyRootOperatorMayGenerateInvoices' for en-US — check ResourcesPath/resx manifest name.");
            en.Value.ShouldBe("Only the root operator may generate invoices across tenants.");

            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            var pt = localizer["Billing.OnlyRootOperatorMayGenerateInvoices"];
            pt.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Billing.OnlyRootOperatorMayGenerateInvoices' for pt-BR — check the .pt-BR catalog manifest name.");
            pt.Value.ShouldBe("Apenas o operador raiz pode gerar faturas entre tenants.");

            pt.Value.ShouldNotBe(en.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
