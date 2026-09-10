using System.Globalization;
using Spectre.Console;

namespace FSH.CLI.Infrastructure;

/// <summary>
/// Removes <c>FSH.Framework.*</c> from the NuGet global-packages cache.
/// </summary>
/// <remarks>
/// Shared by <c>fsh framework pack --clear-cache</c> and <c>fsh framework clean-cache</c>.
/// This is the fix for the most common local-feed failure: NuGet keys its cache on
/// id + version, so a rebuilt package that reuses a version is never re-extracted and the
/// consuming project silently keeps compiling against the old bits.
/// </remarks>
internal static class FrameworkCacheCleaner
{
    internal static async Task<int> ClearAsync(bool dryRun, CancellationToken cancellationToken)
    {
        string? globalPackages = await FrameworkFeed.GetGlobalPackagesFolderAsync(cancellationToken).ConfigureAwait(false);

        if (globalPackages is null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Could not determine the NuGet global-packages folder; nothing cleared.[/]");
            return 1;
        }

        // Package folders on disk are lower-cased by NuGet.
        string prefix = FshConstants.FrameworkPackagePrefix.ToUpperInvariant();
        var targets = Directory
            .EnumerateDirectories(globalPackages)
            .Where(directory => Path.GetFileName(directory)
                .ToUpperInvariant()
                .StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(directory => directory, StringComparer.Ordinal)
            .ToList();

        if (targets.Count == 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]No {FshConstants.FrameworkPackagePrefix}* packages in the cache.[/]");
            return 0;
        }

        int removed = 0;
        foreach (string target in targets)
        {
            string name = Path.GetFileName(target);

            if (dryRun)
            {
                AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]would remove[/] {name.EscapeMarkup()}");
                removed++;
                continue;
            }

            try
            {
                Directory.Delete(target, recursive: true);
                AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]removed[/] {name.EscapeMarkup()}");
                removed++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine($"  [{FshConstants.WarningColor}]skipped[/] {name.EscapeMarkup()}: {ex.Message.EscapeMarkup()}");
            }
        }

        AnsiConsole.MarkupLine(
            dryRun
                ? $"[{FshConstants.DimColor}]{removed.ToString(CultureInfo.InvariantCulture)} cached package(s) would be removed.[/]"
                : $"[{FshConstants.SuccessColor}]Cleared {removed.ToString(CultureInfo.InvariantCulture)} cached package(s).[/]");

        return 0;
    }
}
