namespace FSH.CLI.Infrastructure;

/// <summary>
/// Minimal semantic-version comparison for the CLI's "update available" hints.
/// Purpose-built so a locally-built CLI/template that is AHEAD of what's published
/// (e.g. a 10.0.0 dev build vs a 10.0.0-rc.2 on NuGet) never nags about a phantom
/// update — the old code compared with string inequality, which also tripped over
/// the `+gitsha` build-metadata suffix on the informational version.
/// </summary>
internal static class VersionComparer
{
    /// <summary>
    /// True only when <paramref name="latest"/> is a strictly higher semantic
    /// version than <paramref name="current"/>. Build metadata (<c>+...</c>) is
    /// ignored; a stable release outranks any pre-release of the same core
    /// version (per SemVer precedence).
    /// </summary>
    internal static bool IsNewer(string? latest, string? current)
    {
        if (string.IsNullOrWhiteSpace(latest)) return false;
        if (string.IsNullOrWhiteSpace(current)) return true;

        (Version latestCore, string latestPre) = Parse(latest);
        (Version currentCore, string currentPre) = Parse(current);

        int coreComparison = latestCore.CompareTo(currentCore);
        if (coreComparison != 0) return coreComparison > 0;

        // Same core version: a stable build (no pre-release tag) ranks above any
        // pre-release; between two pre-releases, compare the tags ordinally.
        bool latestStable = latestPre.Length == 0;
        bool currentStable = currentPre.Length == 0;
        if (latestStable && currentStable) return false;            // identical
        if (latestStable) return true;                              // stable > pre-release
        if (currentStable) return false;                            // pre-release < stable
        return string.CompareOrdinal(latestPre, currentPre) > 0;    // both pre-release
    }

    // Splits "10.0.0-rc.2+abc123" into (Version 10.0.0, "rc.2"), dropping build metadata.
    private static (Version Core, string PreRelease) Parse(string version)
    {
        int plus = version.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0) version = version[..plus];

        string pre = string.Empty;
        int dash = version.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
        {
            pre = version[(dash + 1)..];
            version = version[..dash];
        }

        return (Version.TryParse(version, out Version? core) ? core : new Version(0, 0, 0), pre);
    }
}
