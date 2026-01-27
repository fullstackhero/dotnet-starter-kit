using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using FSH.CLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

/// <summary>
/// Display CLI and project version information.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli via reflection")]
internal sealed class VersionCommand : AsyncCommand<VersionCommand.Settings>
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli via reflection")]
    internal sealed class Settings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the FSH project (defaults to current directory)")]
        [DefaultValue(".")]
        public string Path { get; set; } = ".";

        [CommandOption("--json")]
        [Description("Output as JSON")]
        [DefaultValue(false)]
        public bool Json { get; set; }
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var cliVersion = GetCliVersion();
        var manifest = FshManifest.TryLoad(settings.Path);

        if (settings.Json)
        {
            OutputJson(cliVersion, manifest);
        }
        else
        {
            OutputTable(cliVersion, manifest, settings.Path);
        }

        return Task.FromResult(0);
    }

    private static void OutputTable(string cliVersion, FshManifest? manifest, string path)
    {
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Component[/]")
            .AddColumn("[blue]Version[/]");

        table.AddRow("FSH CLI", $"[green]{cliVersion}[/]");

        if (manifest != null)
        {
            table.AddRow("Project FSH Version", $"[green]{manifest.FshVersion}[/]");
            table.AddRow("Project Created", $"[dim]{manifest.CreatedAt:yyyy-MM-dd HH:mm}[/]");
            table.AddRow("Project Type", $"[dim]{manifest.Options.Type}[/]");
            table.AddRow("Architecture", $"[dim]{manifest.Options.Architecture}[/]");
            table.AddRow("Database", $"[dim]{manifest.Options.Database}[/]");

            if (manifest.Options.Modules.Count > 0)
            {
                table.AddRow("Modules", $"[dim]{string.Join(", ", manifest.Options.Modules)}[/]");
            }

            if (manifest.LastUpgradeAt.HasValue)
            {
                table.AddRow("Last Upgrade", $"[dim]{manifest.LastUpgradeAt:yyyy-MM-dd HH:mm}[/]");
            }

            // Show building blocks versions
            AnsiConsole.Write(table);

            if (manifest.Tracking.BuildingBlocks.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[blue]Building Blocks:[/]");
                var bbTable = new Table()
                    .Border(TableBorder.Simple)
                    .AddColumn("Package")
                    .AddColumn("Version");

                foreach (var (package, version) in manifest.Tracking.BuildingBlocks)
                {
                    bbTable.AddRow($"[dim]{package}[/]", version);
                }
                AnsiConsole.Write(bbTable);
            }
        }
        else
        {
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]No FSH project found at:[/] [yellow]{Path.GetFullPath(path)}[/]");
            AnsiConsole.MarkupLine("[dim]Run [green]fsh new[/] to create a new project.[/]");
        }

        AnsiConsole.WriteLine();
    }

    private static void OutputJson(string cliVersion, FshManifest? manifest)
    {
        var output = new
        {
            cliVersion,
            project = manifest != null ? new
            {
                fshVersion = manifest.FshVersion,
                createdAt = manifest.CreatedAt,
                type = manifest.Options.Type,
                architecture = manifest.Options.Architecture,
                database = manifest.Options.Database,
                modules = manifest.Options.Modules,
                buildingBlocks = manifest.Tracking.BuildingBlocks,
                lastUpgradeAt = manifest.LastUpgradeAt
            } : null
        };

        AnsiConsole.WriteLine(System.Text.Json.JsonSerializer.Serialize(output, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        }));
    }

    private static string GetCliVersion()
    {
        var assembly = typeof(VersionCommand).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0";
    }
}
