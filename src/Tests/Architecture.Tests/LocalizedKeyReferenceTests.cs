using FSH.Framework.Core.Localization;
using Shouldly;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Code → resx guard: every literal key the code hands to <c>CustomException.Localized</c>, to a
/// localization wrapper's <c>MessageKey</c> or to an <c>IStringLocalizer&lt;T&gt;</c> must exist in the
/// neutral catalog it targets, and that catalog entry's <c>{n}</c> placeholders must fit the
/// arguments passed with it.
///
/// Neither mistake is visible at runtime: GlobalExceptionHandler answers a missing key or a
/// FormatException with the English exception message, and IStringLocalizer answers a missing key
/// with the key itself. A typo, or a key added to the wrong module's catalog, ships green otherwise.
///
/// The scan reads source text, not syntax trees, so it only sees string-literal keys. A key passed
/// through a variable is listed in <see cref="NonLiteralKeySites"/> and reviewed by hand.
/// </summary>
public sealed partial class LocalizedKeyReferenceTests
{
    private const string SharedCatalog = nameof(SharedResources);

    // ponytail: RoleService.EnsureNotSystemRole receives its key as a parameter, so its callers'
    // literal keys are not checked here. If more helpers like it appear, scan the callers instead.
    private static readonly string[] NonLiteralKeySites =
    [
        "src/BuildingBlocks/Core/Exceptions/CustomException.cs: string key",
        "src/Modules/Identity/Modules.Identity/Features/v1/Roles/RoleService.cs: messageKey",
    ];

    [Fact]
    public void Every_Literal_Resource_Key_Exists_In_Its_Catalog_With_Enough_Arguments()
    {
        var scan = new Scan(LoadNeutralCatalogs());
        foreach (var (path, source) in SourceFiles())
        {
            scan.FactoryCalls(path, source);
            scan.WrapperKeys(path, source);
            scan.IndexerCalls(path, source);
        }

        // A pattern that stops matching would leave this test asserting nothing, and green. The
        // floors sit well under today's counts (≈300, 5 and ≈100) so routine deletions still pass.
        scan.FactoryCallCount.ShouldBeGreaterThan(250, "the .Localized(...) scan found too few calls; the pattern probably broke");
        scan.WrapperKeyCount.ShouldBeGreaterThan(3, "the MessageKey initializer scan found too few wrappers; the pattern probably broke");
        scan.IndexerCallCount.ShouldBeGreaterThan(80, "the localizer[\"...\"] scan found too few calls; the pattern probably broke");

        scan.NonLiteral.Distinct().Order(StringComparer.Ordinal).ToArray().ShouldBe(
            NonLiteralKeySites,
            "a key passed through a variable cannot be checked against its catalog; pass a literal, " +
            "or add the site to NonLiteralKeySites after checking its callers");

        scan.Violations.ShouldBeEmpty(
            "Every key must exist in the neutral (English) resx of the catalog it targets, and that " +
            "entry's {n} placeholders must fit the arguments passed with it. Violations:\n  " +
            string.Join("\n  ", scan.Violations));
    }

    // The argument count drives the placeholder check, so a comma inside a literal or a comment must
    // not split an argument, and an apostrophe in a comment must not open a char literal.
    [Theory]
    [InlineData("(\"K\", a, b)", 3)]
    [InlineData("(\"K\", // the user's id, not a separator\n a)", 2)]
    [InlineData("(\"K\", /* x, y */ a)", 2)]
    [InlineData("(\"K\", $\"{a}, {{b}}\", 'x', ',')", 4)]
    [InlineData("(\"K\", @\"say \"\"hi, there\"\"\")", 2)]
    [InlineData("(\"K\", string.Join(\", \", ids))", 2)]
    public void Argument_Splitter_Ignores_Commas_Inside_Literals_Brackets_And_Comments(string call, int expected)
    {
        ArgumentNullException.ThrowIfNull(call);
        SplitArguments(call, 0).Count.ShouldBe(expected);
    }

    private sealed class Scan(Dictionary<string, Dictionary<string, string>> catalogs)
    {
        public List<string> Violations { get; } = [];

        public List<string> NonLiteral { get; } = [];

        public int FactoryCallCount { get; private set; }

        public int WrapperKeyCount { get; private set; }

        public int IndexerCallCount { get; private set; }

        public void FactoryCalls(string path, string source)
        {
            foreach (var call in Code(FactoryCallRegex(), source))
            {
                var args = SplitArguments(source, call.Index + call.Length - 1);
                if (!TryReadLiteral(args[0], out var key))
                {
                    NonLiteral.Add($"{path}: {args[0]}");
                    continue;
                }

                FactoryCallCount++;
                // Catalogs are keyed by the marker's short name, so a qualified `Ns.XResources` keeps its last segment.
                var catalog = call.Groups["catalog"].Success ? call.Groups["catalog"].Value.Split('.')[^1] : SharedCatalog;
                Check(Where(path, source, call.Index), catalog, key, args.Count - 1);
            }
        }

        public void WrapperKeys(string path, string source)
        {
            foreach (var assignment in Code(WrapperKeyRegex(), source))
            {
                WrapperKeyCount++;
                var open = source.LastIndexOf('{', assignment.Index);
                var body = source[(open + 1)..MatchingClose(source, open)];
                var resourceSource = ResourceSourceRegex().Match(body);
                var catalog = resourceSource.Success ? resourceSource.Groups["catalog"].Value : SharedCatalog;
                var argsAt = body.IndexOf("MessageArgs", StringComparison.Ordinal);
                var argCount = argsAt < 0 ? 0 : SplitArguments(body, body.IndexOf('[', argsAt)).Count;
                Check(Where(path, source, assignment.Index), catalog, assignment.Groups["key"].Value, argCount);
            }
        }

        public void IndexerCalls(string path, string source)
        {
            var localizers = LocalizerFields(source);
            foreach (var index in Code(IndexerRegex(), source))
            {
                var name = index.Groups["name"].Value;
                var where = Where(path, source, index.Index);
                if (!localizers.TryGetValue(name, out var catalog))
                {
                    Violations.Add($"{where} '{name}' is not declared as an IStringLocalizer<T> in this file, so its catalog is unknown");
                    continue;
                }

                if (catalog is null)
                {
                    Violations.Add($"{where} '{name}' is declared with two different catalogs in this file");
                    continue;
                }

                IndexerCallCount++;
                var args = SplitArguments(source, index.Index + name.Length);
                Check(where, catalog, index.Groups["key"].Value, args.Count - 1);
            }
        }

        private void Check(string where, string catalog, string key, int argCount)
        {
            if (!catalogs.TryGetValue(catalog, out var entries))
            {
                Violations.Add($"{where} '{key}': '{catalog}' is not an embedded resx catalog");
                return;
            }

            if (!entries.TryGetValue(key, out var value))
            {
                Violations.Add($"{where} '{key}' is missing from {catalog}.resx");
                return;
            }

            var indexes = CatalogParityTests.PlaceholderIndexes(value);
            var needed = indexes.Count == 0 ? 0 : indexes.Max + 1;
            if (needed > argCount)
            {
                Violations.Add($"{where} '{key}' in {catalog}.resx uses {needed} argument(s) but the call passes {argCount}");
            }
        }
    }

    private static Dictionary<string, Dictionary<string, string>> LoadNeutralCatalogs()
    {
        var catalogs = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var marker in CatalogParityTests.DiscoverCatalogMarkers())
        {
            // Call sites name the marker by its short name; two markers sharing one would make
            // the lookup pick either catalog.
            catalogs.ContainsKey(marker.Name).ShouldBeFalse($"two catalog markers are named {marker.Name}");
            catalogs[marker.Name] = CatalogParityTests.OwnEntries(new ResourceManager(marker), CultureInfo.InvariantCulture) ?? [];
        }

        return catalogs;
    }

    private static IEnumerable<(string Path, string Source)> SourceFiles()
    {
        var root = ModuleArchitectureTestsFixture.SolutionRoot;
        return new[] { "BuildingBlocks", "Modules" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, "src", folder), "*.cs", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(relative => !relative.Contains("/bin/", StringComparison.Ordinal) && !relative.Contains("/obj/", StringComparison.Ordinal))
            .Select(relative => (relative, File.ReadAllText(Path.Combine(root, relative))));
    }

    /// <summary>Matches outside <c>//</c> and <c>///</c> comment lines, where XML docs mention the API.</summary>
    private static IEnumerable<Match> Code(Regex pattern, string source) =>
        pattern.Matches(source).Where(match =>
        {
            var lineStart = source.LastIndexOf('\n', Math.Max(0, match.Index - 1)) + 1;
            return !source.AsSpan(lineStart, match.Index - lineStart).TrimStart().StartsWith("//", StringComparison.Ordinal);
        });

    private static string Where(string path, string source, int at) =>
        $"{path}:{source.AsSpan(0, at).Count('\n') + 1}";

    /// <summary>
    /// Maps each IStringLocalizer&lt;T&gt; field or parameter in the file to T's short name; a name
    /// bound to two different catalogs maps to null.
    /// </summary>
    private static Dictionary<string, string?> LocalizerFields(string source)
    {
        var fields = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (Match declaration in LocalizerDeclarationRegex().Matches(source))
        {
            var name = declaration.Groups["name"].Value;
            var catalog = declaration.Groups["catalog"].Value;
            fields[name] = fields.TryGetValue(name, out var existing) && existing != catalog ? null : catalog;
        }

        return fields;
    }

    /// <summary>
    /// The top-level, comma-separated arguments between the bracket at <paramref name="open"/> and
    /// its match, with string literals and nested brackets kept whole.
    /// </summary>
    private static List<string> SplitArguments(string source, int open)
    {
        var close = MatchingClose(source, open);
        var args = new List<string>();
        var start = open + 1;
        for (var i = open + 1; i < close; i = Next(source, i))
        {
            if (source[i] == ',')
            {
                args.Add(source[start..i].Trim());
                start = i + 1;
            }
        }

        var last = source[start..close].Trim();
        if (last.Length > 0 || args.Count > 0)
        {
            args.Add(last);
        }

        return args;
    }

    private static int MatchingClose(string source, int open)
    {
        var closer = source[open] switch { '(' => ')', '[' => ']', _ => '}' };
        for (var i = open + 1; i < source.Length; i = Next(source, i))
        {
            if (source[i] == closer)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"unbalanced '{source[open]}' at offset {open}");
    }

    /// <summary>
    /// The index after the token at <paramref name="i"/>, stepping over whole literals, bracket groups and
    /// comments, so a comma, quote or apostrophe inside a comment within a call is not read as code.
    /// </summary>
    private static int Next(string source, int i) => source[i] switch
    {
        '"' or '\'' => SkipLiteral(source, i) + 1,
        '(' or '[' or '{' => MatchingClose(source, i) + 1,
        '/' when i + 1 < source.Length && source[i + 1] is '/' or '*' => SkipComment(source, i),
        _ => i + 1,
    };

    private static int SkipComment(string source, int slash)
    {
        if (source[slash + 1] == '/')
        {
            var lineEnd = source.IndexOf('\n', slash);
            return lineEnd < 0 ? source.Length : lineEnd;
        }

        var blockEnd = source.IndexOf("*/", slash + 2, StringComparison.Ordinal);
        return blockEnd < 0
            ? throw new InvalidOperationException($"unterminated comment at offset {slash}")
            : blockEnd + 2;
    }

    /// <summary>Index of the quote closing the literal that opens at <paramref name="quote"/>.</summary>
    private static int SkipLiteral(string source, int quote)
    {
        var prefix = source[Math.Max(0, quote - 2)..quote];
        var verbatim = prefix.EndsWith('@') || prefix == "@$";
        var interpolated = prefix.EndsWith('$') || prefix == "$@";
        var i = quote + 1;
        while (i < source.Length)
        {
            var c = source[i];
            if (c == source[quote] && !(verbatim && i + 1 < source.Length && source[i + 1] == c))
            {
                return i;
            }

            i += c switch
            {
                '\\' when !verbatim => 2,
                '{' when interpolated && source[i + 1] != '{' => MatchingClose(source, i) - i + 1,
                _ when c == source[quote] || (c is '{' && interpolated) => 2,
                _ => 1,
            };
        }

        throw new InvalidOperationException($"unterminated literal at offset {quote}");
    }

    private static bool TryReadLiteral(string argument, out string value)
    {
        var match = PlainLiteralRegex().Match(argument);
        value = match.Success ? match.Groups["value"].Value : string.Empty;
        return match.Success;
    }

    // `.Localized<IdentityResources>(`, `.Localized<Ns.IdentityResources> (` and `.Localized(`. Requiring
    // `<` or `(` right after the name keeps LocalizedKeyNotFoundException and its siblings out.
    [GeneratedRegex(@"(?<![\w])Localized\s*(?:<\s*(?<catalog>[\w.]+)\s*>)?\s*\(")]
    private static partial Regex FactoryCallRegex();

    // The localization wrappers around BCL exceptions keep the initializer form.
    [GeneratedRegex(@"MessageKey\s*=\s*""(?<key>[^""]+)""")]
    private static partial Regex WrapperKeyRegex();

    [GeneratedRegex(@"ResourceSource\s*=\s*typeof\((?<catalog>\w+)\)")]
    private static partial Regex ResourceSourceRegex();

    // A field or parameter (`IStringLocalizer<X> localizer`) or a local resolved from DI
    // (`var localizer = services.GetRequiredService<IStringLocalizer<X>>()`).
    [GeneratedRegex(@"IStringLocalizer<(?<catalog>\w+)>\??\s+(?<name>\w+)|var\s+(?<name>\w+)\s*=[^;]*?IStringLocalizer<(?<catalog>\w+)>")]
    private static partial Regex LocalizerDeclarationRegex();

    [GeneratedRegex(@"(?<name>\w*[Ll]ocalizer)\[\s*""(?<key>[^""]+)""")]
    private static partial Regex IndexerRegex();

    [GeneratedRegex(@"^""(?<value>[^""\\]*)""$")]
    private static partial Regex PlainLiteralRegex();
}
