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
    /// Gets the installed version of the FSH dotnet new template, or null.
    /// </summary>
    internal static async Task<string?> GetInstalledTemplateVersionAsync(
        CancellationToken cancellationToken = default)
    {
        // dotnet new list outputs a table. We use --columns to get a parseable format.
        (bool ok, string output) = await ProcessRunner.CaptureAsync(
            "dotnet", $"new list {FshConstants.TemplateShortName}",
            cancellationToken).ConfigureAwait(false);

        if (!ok) return null;

        foreach (string line in output.Split('\n'))
        {
            if (!line.Contains(FshConstants.TemplateShortName, StringComparison.OrdinalIgnoreCase)
                || !line.Contains("FullStackHero", StringComparison.OrdinalIgnoreCase))
                continue;

            // Version appears as a column in the dotnet new list table output.
            // Columns are separated by 2+ spaces. Find the segment that looks like a version.
            string[] segments = line.Split("  ", StringSplitOptions.RemoveEmptyEntries);
            foreach (string segment in segments)
            {
                string trimmed = segment.Trim();
                if (trimmed.Length > 0 && char.IsDigit(trimmed[0]) && trimmed.Contains('.', StringComparison.Ordinal))
                {
                    return trimmed;
                }
            }
        }

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
