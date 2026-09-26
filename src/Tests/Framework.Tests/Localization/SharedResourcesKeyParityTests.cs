using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Framework.Tests.Localization;

// Guards against an English key missing from the .pt-BR catalog (a silent English fallback shipped as
// "translated"). Enumerates each culture's own embedded resx (includeParentCultures: false) and
// asserts identical key sets.
public sealed class SharedResourcesKeyParityTests
{
    private static List<string> KeysFor(string culture) =>
        EntriesFor(culture).Select(e => e.Key).ToList();

    private static Dictionary<string, string> EntriesFor(string culture)
    {
        var localizer = SharedResourcesLocalizerFactory.Create();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = culture.Length == 0
                ? CultureInfo.InvariantCulture
                : new CultureInfo(culture);
            return localizer.GetAllStrings(includeParentCultures: false)
                .ToDictionary(s => s.Name, s => s.Value, StringComparer.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // {0}, {1:N2}, {0,-10} — the index is what has to match; alignment and format do not.
    private static readonly Regex PlaceholderPattern = new(@"\{(\d+)(?:[,:][^}]*)?\}", RegexOptions.Compiled);

    private static string PlaceholderSignature(string value) =>
        string.Join(
            ",",
            PlaceholderPattern.Matches(value)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(i => int.Parse(i, CultureInfo.InvariantCulture)));

    [Fact]
    public void Neutral_and_ptBR_catalogs_have_matching_keys()
    {
        var neutral = KeysFor(string.Empty);   // SharedResources.resx (English / fallback)
        var pt = KeysFor("pt-BR");                 // SharedResources.pt-BR.resx

        neutral.ShouldNotBeEmpty();
        pt.OrderBy(k => k, StringComparer.Ordinal)
            .ShouldBe(neutral.OrderBy(k => k, StringComparer.Ordinal));
    }

    // Matching keys are not enough. A translation that drops {0}, or renumbers it, either
    // swallows the argument or throws FormatException at the point the message is built —
    // and neither shows up as a missing key.
    [Fact]
    public void Neutral_and_ptBR_messages_take_the_same_arguments()
    {
        var neutral = EntriesFor(string.Empty);
        var pt = EntriesFor("pt-BR");

        var divergent = neutral
            .Where(entry => pt.ContainsKey(entry.Key))
            .Select(entry => new
            {
                entry.Key,
                Neutral = PlaceholderSignature(entry.Value),
                PtBR = PlaceholderSignature(pt[entry.Key]),
            })
            .Where(x => !string.Equals(x.Neutral, x.PtBR, StringComparison.Ordinal))
            .Select(x => $"{x.Key}: neutral [{x.Neutral}] vs pt-BR [{x.PtBR}]")
            .ToList();

        divergent.ShouldBeEmpty();
    }
}
