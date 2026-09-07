using System.Globalization;
using System.Text.RegularExpressions;

namespace FSH.CLI.Infrastructure;

/// <summary>
/// Shared resolution logic for the local NuGet feed that serves the opt-in
/// <c>FSH.Framework.*</c> packages. Used by both the producing side
/// (<c>fsh framework pack</c>) and the consuming side (<c>fsh new --framework-packages</c>)
/// so the two can never disagree about where packages live.
/// </summary>
internal static partial class FrameworkFeed
{
    // Splits "FSH.Framework.Eventing.Abstractions.10.0.0-local.20260901T143000" into id and
    // version. Anchoring the version on the first "major.minor.patch" triple keeps both dotted
    // package ids (Eventing.Abstractions) and dotted prerelease labels (-local.<stamp>) intact.
    [GeneratedRegex(@"^(?<id>.+?)\.(?<version>\d+\.\d+\.\d+(?:[-+].*)?)$", RegexOptions.ExplicitCapture)]
    private static partial Regex PackageFileName { get; }

    /// <summary>
    /// Parses a .nupkg path into its package id and version, or <see langword="null"/> if the
    /// file name does not look like a NuGet package.
    /// </summary>
    internal static (string Id, string Version)? ParsePackageFileName(string path)
    {
        Match match = PackageFileName.Match(Path.GetFileNameWithoutExtension(path));

        return match.Success
            ? (match.Groups["id"].Value, match.Groups["version"].Value)
            : null;
    }

    /// <summary>
    /// Newest <c>FSH.Framework.Core</c> version in the feed, by pack time. Used to default
    /// the version a scaffolded project pins, so the common case needs no version flag at all.
    /// </summary>
    internal static string? GetLatestVersion(string feed)
    {
        if (!Directory.Exists(feed))
            return null;

        return Directory
            .EnumerateFiles(feed, $"{FshConstants.FrameworkPackagePrefix}Core.*.nupkg")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(path => ParsePackageFileName(path)?.Version)
            .FirstOrDefault(version => version is not null);
    }

    /// <summary>
    /// Resolves the feed directory: explicit path, then <c>FSH_LOCAL_FEED</c>, then
    /// <c>~/.fsh/local-nuget</c>. Never returns a relative path.
    /// </summary>
    internal static string Resolve(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);

        string? fromEnvironment = Environment.GetEnvironmentVariable(FshConstants.LocalFeedEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return Path.GetFullPath(fromEnvironment);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".fsh",
            "local-nuget");
    }

    /// <summary>
    /// Builds a unique, sortable prerelease version, e.g. <c>10.0.0-local.20260901T143000</c>.
    /// </summary>
    /// <remarks>
    /// NuGet caches by id+version in the global-packages folder, so re-publishing the same
    /// version with changed content silently serves stale bits — the classic local-feed trap.
    /// A fresh version per pack sidesteps it entirely. Build metadata (<c>+sha</c>) cannot be
    /// used instead: SemVer ignores it when comparing versions. The literal <c>T</c> keeps the
    /// timestamp an alphanumeric identifier, which sorts lexically and avoids any ambiguity
    /// around very long numeric prerelease identifiers.
    /// </remarks>
    internal static string NewLocalVersion(string baseVersion = "10.0.0")
    {
        // Read the clock once: two reads could straddle midnight and stamp a date that does
        // not belong to the time beside it.
        DateTime timestamp = DateTime.UtcNow;

        return string.Create(CultureInfo.InvariantCulture, $"{baseVersion}-local.{timestamp:yyyyMMdd}T{timestamp:HHmmss}");
    }

    /// <summary>
    /// Asks the SDK where the global-packages folder is, rather than assuming
    /// <c>~/.nuget/packages</c> — it is relocatable via <c>NUGET_PACKAGES</c> and nuget.config.
    /// </summary>
    internal static async Task<string?> GetGlobalPackagesFolderAsync(CancellationToken cancellationToken)
    {
        (bool success, string output) = await ProcessRunner
            .CaptureAsync("dotnet", "nuget locals global-packages --list", cancellationToken)
            .ConfigureAwait(false);

        if (!success || string.IsNullOrWhiteSpace(output))
            return null;

        // Output is "global-packages: <path>" (or "info : global-packages: <path>" on some SDKs).
        // Split on the LAST colon that precedes a path so a Windows drive letter survives.
        foreach (string line in output.Split('\n'))
        {
            int marker = line.IndexOf("global-packages:", StringComparison.OrdinalIgnoreCase);
            if (marker < 0) continue;

            string path = line[(marker + "global-packages:".Length)..].Trim();
            if (path.Length > 0 && Directory.Exists(path))
                return path;
        }

        return null;
    }
}
