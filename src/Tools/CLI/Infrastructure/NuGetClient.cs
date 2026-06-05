using System.Text.Json;

namespace FSH.CLI.Infrastructure;

/// <summary>
/// Lightweight NuGet API client for version checking.
/// Uses a static HttpClient to avoid socket exhaustion.
/// </summary>
internal static class NuGetClient
{
    private static readonly HttpClient Http = CreateClient();

    /// <summary>
    /// Gets the latest stable version for a package, or null if unavailable.
    /// Falls back to latest pre-release if no stable version exists.
    /// </summary>
    internal static async Task<string?> GetLatestVersionAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // NuGet API requires lowercase package IDs
#pragma warning disable CA1308 // NuGet flat container API requires lowercase
            string url = $"{FshConstants.NuGetFlatContainerUrl}/{packageId.ToLowerInvariant()}/index.json";
#pragma warning restore CA1308
            using var response = await Http.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!doc.RootElement.TryGetProperty("versions", out JsonElement versions))
                return null;

            // Walk backwards to find the latest version.
            // Prefer stable (no '-' in version string) over pre-release.
            string? latestStable = null;
            string? latestAny = null;

            foreach (JsonElement v in versions.EnumerateArray())
            {
                string? version = v.GetString();
                if (version is null) continue;

                latestAny = version;
                if (!version.Contains('-', StringComparison.Ordinal))
                {
                    latestStable = version;
                }
            }

            return latestStable ?? latestAny;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the installed version of the FSH dotnet new template, or null if it is not
    /// installed. Returns "local" when installed from a folder (no package version).
    /// </summary>
    internal static async Task<string?> GetInstalledTemplateVersionAsync(
        CancellationToken cancellationToken = default)
    {
        // `dotnet new list` has no version column; `dotnet new uninstall` (no args) lists installed
        // template packages with their versions (package id line, then an indented "Version: x.y.z").
        (bool ok, string output) = await ProcessRunner.CaptureAsync(
            "dotnet", "new uninstall", cancellationToken).ConfigureAwait(false);

        if (!ok) return null;

        string[] lines = output.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            // Match the package-block header: the package id on its own (indented) line.
            if (!string.Equals(lines[i].Trim(), FshConstants.TemplatePackageId, StringComparison.OrdinalIgnoreCase))
                continue;

            // The next indented line is "Version: x.y.z" for NuGet-installed packages.
            for (int j = i + 1; j < Math.Min(i + 3, lines.Length); j++)
            {
                int idx = lines[j].IndexOf("Version:", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    return lines[j][(idx + "Version:".Length)..].Trim();
            }

            return "local"; // installed, but no package version reported
        }

        // A folder install lists a path rather than the package id — detect via the template entry.
        if (output.Contains("FullStackHero", StringComparison.OrdinalIgnoreCase)
            && output.Contains($"({FshConstants.TemplateShortName})", StringComparison.OrdinalIgnoreCase))
            return "local";

        return null;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FSH-CLI/1.0");
        client.Timeout = TimeSpan.FromSeconds(10);
        return client;
    }
}
