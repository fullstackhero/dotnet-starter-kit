using FSH.Framework.Core.Localization;
using Shouldly;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Generic key-parity guard across EVERY resx catalog, discovered by reflection.
///
/// Each module already has a hand-written parity test, but those only cover the modules
/// someone remembered to write one for — Notifications shipped a catalog with no parity
/// test at all, and has no test project to put one in. This closes that class of gap: a
/// new module catalog is covered the moment its assembly lands in the output, with no new
/// test and no new test project.
///
/// Parity matters because a key missing from a translated catalog does not fail — resource
/// fallback quietly serves the neutral (English) string, so an untranslated message ships
/// looking translated.
/// </summary>
public sealed class CatalogParityTests
{
    /// <summary>
    /// A catalog marker is a type with an embedded `.resources` manifest matching its own
    /// full name — which is exactly the co-located `ResourcesPath = ""` convention the
    /// framework relies on. Anything else named `*Resources` is skipped.
    /// </summary>
    private static List<Type> DiscoverCatalogMarkers()
    {
        var assemblies = ModuleAssemblyDiscovery.GetModuleAssemblies()
            .Append(typeof(SharedResources).Assembly)
            .Distinct()
            .ToList();

        var markers = new List<Type>();
        foreach (var assembly in assemblies)
        {
            var manifests = assembly.GetManifestResourceNames();
            foreach (var type in SafeGetTypes(assembly))
            {
                if (!type.IsClass || type.FullName is null) continue;
                if (!type.Name.EndsWith("Resources", StringComparison.Ordinal)) continue;
                if (manifests.Contains($"{type.FullName}.resources", StringComparer.Ordinal))
                {
                    markers.Add(type);
                }
            }
        }

        return markers.OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }

    /// <summary>
    /// Keys declared by this culture's OWN catalog. `tryParents: false` is the whole point:
    /// with parent fallback on, a missing pt-BR key would be answered by the neutral catalog
    /// and parity would look perfect while half the strings were English.
    /// </summary>
    private static Dictionary<string, string>? OwnEntries(ResourceManager manager, CultureInfo culture)
    {
        var set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        if (set is null) return null;

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key)
            {
                entries[key] = entry.Value as string ?? string.Empty;
            }
        }

        return entries;
    }

    /// <summary>
    /// The `{0}`-style argument indexes a message consumes. Escaped braces (`{{`, `}}`) are
    /// stripped first so a literal brace is not mistaken for a placeholder.
    /// </summary>
    private static SortedSet<int> PlaceholderIndexes(string value)
    {
        var unescaped = value.Replace("{{", string.Empty, StringComparison.Ordinal)
            .Replace("}}", string.Empty, StringComparison.Ordinal);

        var indexes = new SortedSet<int>();
        foreach (var match in PlaceholderPattern.Matches(unescaped).Cast<Match>())
        {
            indexes.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
        }

        return indexes;
    }

    private static readonly Regex PlaceholderPattern =
        new(@"\{(\d+)(?::[^}]*)?\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Every_Catalog_Has_Matching_Keys_In_Every_Supported_Culture()
    {
        var markers = DiscoverCatalogMarkers();

        // Never let a discovery regression read as a pass: if the reflection stops finding
        // catalogs, this test would otherwise assert nothing and go green.
        markers.Count.ShouldBeGreaterThanOrEqualTo(
            11,
            "expected the Core catalog plus one per module; discovery found fewer, so this " +
            "test would silently stop guarding the ones it lost");

        var violations = new List<string>();

        foreach (var marker in markers)
        {
            var manager = new ResourceManager(marker);

            var neutral = OwnEntries(manager, CultureInfo.InvariantCulture);
            if (neutral is null || neutral.Count == 0)
            {
                violations.Add($"{marker.FullName}: neutral catalog is missing or empty");
                continue;
            }

            foreach (var tag in SupportedCultures.Tags)
            {
                // The neutral catalog IS the default culture's catalog; there is no
                // `*.en-US.resx` and there should not be one.
                if (tag == SupportedCultures.Default) continue;

                var translated = OwnEntries(manager, new CultureInfo(tag));
                if (translated is null)
                {
                    violations.Add($"{marker.FullName}: no `.{tag}.resx` catalog at all");
                    continue;
                }

                var missing = neutral.Keys.Except(translated.Keys, StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal).ToList();
                var extra = translated.Keys.Except(neutral.Keys, StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal).ToList();

                if (missing.Count > 0)
                {
                    violations.Add($"{marker.FullName} [{tag}]: missing {missing.Count} key(s) — {string.Join(", ", missing)}");
                }

                if (extra.Count > 0)
                {
                    violations.Add($"{marker.FullName} [{tag}]: {extra.Count} key(s) not in the neutral catalog — {string.Join(", ", extra)}");
                }

                // Matching keys are not enough. The caller passes ONE argument list for every
                // culture, so a translation consuming a different set of `{n}` placeholders than
                // the neutral string either drops data silently or throws FormatException at
                // render time — in the translated culture only, i.e. never on the reviewer's
                // machine. `{1}` present in Portuguese but not English is the dangerous
                // direction: string.Format throws when the index is out of range.
                foreach (var key in neutral.Keys.Intersect(translated.Keys, StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal))
                {
                    var neutralArgs = PlaceholderIndexes(neutral[key]);
                    var translatedArgs = PlaceholderIndexes(translated[key]);

                    if (!neutralArgs.SetEquals(translatedArgs))
                    {
                        violations.Add(
                            $"{marker.FullName} [{tag}] key '{key}': placeholder mismatch — neutral uses " +
                            $"{{{string.Join(",", neutralArgs)}}} but {tag} uses {{{string.Join(",", translatedArgs)}}}");
                    }
                }
            }
        }

        violations.ShouldBeEmpty(
            "Every resx catalog must declare the same keys in every supported culture. A key " +
            "present only in the neutral catalog falls back to English and ships as if it were " +
            "translated. Violations:\n  " + string.Join("\n  ", violations));
    }
}
