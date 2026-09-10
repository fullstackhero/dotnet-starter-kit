using System.ComponentModel;
using FSH.CLI.Infrastructure;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands.Framework;

/// <summary>
/// Purges <c>FSH.Framework.*</c> from the NuGet global-packages cache, so the next restore
/// re-reads them from the feed.
/// </summary>
public sealed class FrameworkCleanCacheCommand : AsyncCommand<FrameworkCleanCacheCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("List what would be removed without deleting anything.")]
        [CommandOption("--dry-run")]
        [DefaultValue(false)]
        public bool DryRun { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return await FrameworkCacheCleaner.ClearAsync(settings.DryRun, cancellationToken).ConfigureAwait(false);
    }
}
