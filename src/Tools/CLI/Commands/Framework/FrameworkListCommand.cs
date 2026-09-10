using System.ComponentModel;
using System.Globalization;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands.Framework;

/// <summary>
/// Lists the <c>FSH.Framework.*</c> packages present in a local feed — the quick answer to
/// "which build is my project actually restoring?".
/// </summary>
public sealed class FrameworkListCommand : AsyncCommand<FrameworkListCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Feed directory to inspect. Defaults to $FSH_LOCAL_FEED, then ~/.fsh/local-nuget.")]
        [CommandOption("-f|--feed")]
        public string? Feed { get; init; }

        [Description("Show every version instead of only the newest of each package.")]
        [CommandOption("--all")]
        [DefaultValue(false)]
        public bool All { get; init; }
    }

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string feed = FrameworkFeed.Resolve(settings.Feed);

        if (!Directory.Exists(feed))
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Feed directory does not exist:[/] {feed.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Create it by running 'fsh framework pack --push'.[/]");
            return Task.FromResult(1);
        }

        var packages = Directory
            .EnumerateFiles(feed, $"{FshConstants.FrameworkPackagePrefix}*.nupkg")
            .Select(path =>
            {
                (string Id, string Version)? parsed = FrameworkFeed.ParsePackageFileName(path);
                return new
                {
                    Id = parsed?.Id ?? Path.GetFileNameWithoutExtension(path),
                    Version = parsed?.Version ?? "?",
                    Modified = File.GetLastWriteTime(path)
                };
            })
            .OrderBy(package => package.Id, StringComparer.Ordinal)
            .ThenByDescending(package => package.Modified)
            .ToList();

        if (packages.Count == 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]No {FshConstants.FrameworkPackagePrefix}* packages in[/] {feed.EscapeMarkup()}");
            return Task.FromResult(0);
        }

        if (!settings.All)
        {
            packages = [.. packages.GroupBy(package => package.Id, StringComparer.Ordinal).Select(group => group.First())];
        }

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("[bold]Package[/]");
        table.AddColumn("[bold]Version[/]");
        table.AddColumn("[bold]Packed[/]");

        foreach (var package in packages)
        {
            table.AddRow(
                package.Id.EscapeMarkup(),
                $"[{FshConstants.AccentColor}]{package.Version.EscapeMarkup()}[/]",
                package.Modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        }

        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Feed:[/] {feed.EscapeMarkup()}");
        AnsiConsole.Write(table);

        if (!settings.All)
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Showing the newest version of each package; use --all to see every version.[/]");

        return Task.FromResult(0);
    }
}
