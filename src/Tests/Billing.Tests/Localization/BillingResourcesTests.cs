using System;
using System.Globalization;
using FSH.Modules.Billing.Contracts;
using System.Linq;
using FSH.Modules.Billing.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Billing.Tests.Localization;

// Proves the BillingResources catalog is embedded under the correct manifest name (ResourcesPath="" =>
// co-located marker + resx). A wrong manifest name flips ResourceNotFound and leaks raw keys; a
// pt-BR key the neutral catalog lacks is an orphan of a rename or delete. Both are caught here.
// A missing pt-BR entry is allowed: it falls back to English.
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
    public void Every_ptBR_key_exists_in_the_neutral_catalog()
    {
        var neutral = KeysFor(string.Empty);   // BillingResources.resx (English / fallback)
        var pt = KeysFor("pt-BR");                 // BillingResources.pt-BR.resx

        neutral.ShouldNotBeEmpty();
        pt.Except(neutral, StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ShouldBeEmpty();
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

    // An enum handed to a message as an argument is looked up as "{EnumType}.{Member}" by
    // GlobalExceptionHandler. A member with no entry falls back to its C# name, which is a code
    // identifier, not display text, so every member needs a neutral entry; pt-BR may omit it and fall back.
    [Theory]
    [InlineData(typeof(TopupRequestStatus))]
    public void Every_enum_member_that_reaches_a_message_has_a_neutral_entry(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        var neutral = KeysFor(string.Empty);

        foreach (var member in Enum.GetNames(enumType))
        {
            var key = $"{enumType.Name}.{member}";
            neutral.ShouldContain(key);
        }
    }
}
