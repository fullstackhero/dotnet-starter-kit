using System;
using System.Globalization;
using FSH.Modules.Tickets.Contracts.Dtos;
using System.Linq;
using FSH.Modules.Tickets.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Tickets.Tests.Localization;

// Proves the TicketsResources catalog is embedded under the correct manifest name so the module
// resx resolves at runtime. A wrong manifest name would flip ResourceNotFound and leak raw keys or
// English text, and a missing pt-BR entry would ship English as if it were translated. Both are caught
// here. This is the module's only unit test project, added when Tickets exception bodies were localized.
public sealed class TicketsResourcesTests
{
    private static IStringLocalizer BuildLocalizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider()
            .GetRequiredService<IStringLocalizerFactory>()
            .Create(typeof(TicketsResources));
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
        var neutral = KeysFor(string.Empty);   // TicketsResources.resx (English / fallback)
        var pt = KeysFor("pt-BR");                 // TicketsResources.pt-BR.resx

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
            var en = localizer["Tickets.ClosedCannotResolve"];
            en.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Tickets.ClosedCannotResolve' for en-US — check ResourcesPath/resx manifest name.");
            en.Value.ShouldBe("A closed ticket cannot be resolved — reopen it first.");

            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            var pt = localizer["Tickets.ClosedCannotResolve"];
            pt.ResourceNotFound.ShouldBeFalse(
                "resx did not resolve 'Tickets.ClosedCannotResolve' for pt-BR — check the .pt-BR catalog manifest name.");
            pt.Value.ShouldBe("Um chamado fechado não pode ser resolvido. Reabra-o primeiro.");

            pt.Value.ShouldNotBe(en.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // TicketNotFound carries the ticket id ({0}); OnlyResolvedCanClose carries the status ({0}).
    [Fact]
    public void Parameterized_keys_format_args_in_both_cultures()
    {
        var localizer = BuildLocalizer();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            localizer["Tickets.TicketNotFound", "abc"].Value.ShouldBe("Ticket abc not found.");

            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            localizer["Tickets.TicketNotFound", "abc"].Value.ShouldBe("Chamado abc não encontrado.");
            // The status argument arrives already localized: GlobalExceptionHandler resolves an enum
            // argument through this same catalog before formatting, so asserting the raw "Open" here
            // would pin a sentence no user ever sees.
            localizer["Tickets.OnlyResolvedCanClose", localizer["TicketStatus.Open"].Value].Value
                .ShouldBe("Somente um chamado resolvido pode ser fechado. O status atual é Aberto. Resolva-o primeiro.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // An enum handed to a message as an argument is looked up as "{EnumType}.{Member}" by
    // GlobalExceptionHandler. A member with no entry falls back to its C# name, which puts an
    // English word inside an otherwise translated sentence, so every member needs both entries.
    [Theory]
    [InlineData(typeof(TicketStatus))]
    public void Every_enum_member_that_reaches_a_message_is_translated(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        var neutral = KeysFor(string.Empty);
        var pt = KeysFor("pt-BR");

        foreach (var member in Enum.GetNames(enumType))
        {
            var key = $"{enumType.Name}.{member}";
            neutral.ShouldContain(key);
            pt.ShouldContain(key);
        }
    }
}
