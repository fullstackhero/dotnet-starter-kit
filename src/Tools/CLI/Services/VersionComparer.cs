using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FSH.CLI.Services;

/// <summary>
/// Compares package versions between current project and latest release.
/// </summary>
internal static partial class VersionComparer
{
    /// <summary>
    /// Parse Directory.Packages.props XML content and extract package versions.
    /// </summary>
    public static Dictionary<string, string> ParsePackagesProps(string xmlContent)
    {
        var packages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var doc = XDocument.Parse(xmlContent);
            var packageVersions = doc.Descendants("PackageVersion");

            foreach (var pv in packageVersions)
            {
                var include = pv.Attribute("Include")?.Value;
                var version = pv.Attribute("Version")?.Value;

                if (!string.IsNullOrEmpty(include) && !string.IsNullOrEmpty(version))
                {
                    packages[include] = version;
                }
            }
        }
        catch (Exception)
        {
            // Return empty dict on parse failure
        }

        return packages;
    }

    /// <summary>
    /// Compare two sets of package versions and return the differences.
    /// </summary>
    public static VersionDiff Compare(
        Dictionary<string, string> currentVersions,
        Dictionary<string, string> latestVersions)
    {
        var diff = new VersionDiff();

        // Check for updates and additions
        foreach (var (package, latestVersion) in latestVersions)
        {
            if (currentVersions.TryGetValue(package, out var currentVersion))
            {
                var comparison = CompareVersions(currentVersion, latestVersion);
                if (comparison < 0)
                {
                    diff.Updated.Add(new PackageUpdate(package, currentVersion, latestVersion, IsBreaking(package, currentVersion, latestVersion)));
                }
            }
            else
            {
                diff.Added.Add(new PackageChange(package, latestVersion));
            }
        }

        // Check for removals
        foreach (var (package, currentVersion) in currentVersions)
        {
            if (!latestVersions.ContainsKey(package))
            {
                diff.Removed.Add(new PackageChange(package, currentVersion));
            }
        }

        return diff;
    }

    /// <summary>
    /// Compare two semantic version strings.
    /// Returns: -1 if v1 &lt; v2, 0 if equal, 1 if v1 &gt; v2
    /// </summary>
    public static int CompareVersions(string v1, string v2)
    {
        // Handle null/empty
        if (string.IsNullOrEmpty(v1) && string.IsNullOrEmpty(v2)) return 0;
        if (string.IsNullOrEmpty(v1)) return -1;
        if (string.IsNullOrEmpty(v2)) return 1;

        // Try to parse as Version first
        var parts1 = ParseVersionParts(v1);
        var parts2 = ParseVersionParts(v2);

        // Compare major.minor.patch
        for (int i = 0; i < Math.Max(parts1.NumericParts.Count, parts2.NumericParts.Count); i++)
        {
            var p1 = i < parts1.NumericParts.Count ? parts1.NumericParts[i] : 0;
            var p2 = i < parts2.NumericParts.Count ? parts2.NumericParts[i] : 0;

            if (p1 < p2) return -1;
            if (p1 > p2) return 1;
        }

        // If numeric parts equal, compare prerelease
        // No prerelease > prerelease (1.0.0 > 1.0.0-beta)
        if (string.IsNullOrEmpty(parts1.Prerelease) && !string.IsNullOrEmpty(parts2.Prerelease))
            return 1;
        if (!string.IsNullOrEmpty(parts1.Prerelease) && string.IsNullOrEmpty(parts2.Prerelease))
            return -1;

        return string.Compare(parts1.Prerelease, parts2.Prerelease, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determine if a version change is potentially breaking.
    /// </summary>
    private static bool IsBreaking(string package, string fromVersion, string toVersion)
    {
        var from = ParseVersionParts(fromVersion);
        var to = ParseVersionParts(toVersion);

        // Major version bump is breaking
        if (to.NumericParts.Count > 0 && from.NumericParts.Count > 0)
        {
            if (to.NumericParts[0] > from.NumericParts[0])
                return true;
        }

        // FSH-specific: certain packages are known to have breaking changes
        // This can be expanded based on release notes
        return false;
    }

    private static VersionParts ParseVersionParts(string version)
    {
        var parts = new VersionParts();

        // Split on dash for prerelease
        var dashIndex = version.IndexOf('-');
        var mainPart = dashIndex >= 0 ? version[..dashIndex] : version;
        parts.Prerelease = dashIndex >= 0 ? version[(dashIndex + 1)..] : string.Empty;

        // Parse numeric parts
        var numericMatches = NumericPartRegex().Matches(mainPart);
        foreach (Match match in numericMatches)
        {
            if (int.TryParse(match.Value, out var num))
            {
                parts.NumericParts.Add(num);
            }
        }

        return parts;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumericPartRegex();

    private sealed class VersionParts
    {
        public List<int> NumericParts { get; } = [];
        public string Prerelease { get; set; } = string.Empty;
    }
}

/// <summary>
/// Result of comparing two sets of package versions.
/// </summary>
internal sealed class VersionDiff
{
    public List<PackageUpdate> Updated { get; } = [];
    public List<PackageChange> Added { get; } = [];
    public List<PackageChange> Removed { get; } = [];

    public bool HasChanges => Updated.Count > 0 || Added.Count > 0 || Removed.Count > 0;
    public int TotalChanges => Updated.Count + Added.Count + Removed.Count;
    public bool HasBreakingChanges => Updated.Any(u => u.IsBreaking);
}

/// <summary>
/// Represents a package version update.
/// </summary>
internal sealed record PackageUpdate(
    string Package,
    string FromVersion,
    string ToVersion,
    bool IsBreaking);

/// <summary>
/// Represents an added or removed package.
/// </summary>
internal sealed record PackageChange(
    string Package,
    string Version);
