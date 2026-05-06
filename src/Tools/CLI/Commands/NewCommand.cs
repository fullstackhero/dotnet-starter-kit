using System.ComponentModel;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

public sealed class NewCommand : AsyncCommand<NewCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Project name (e.g., MyApp). Used for solution, namespaces, and folder names.")]
        [CommandArgument(0, "[name]")]
        public string? Name { get; init; }

        [Description("Output directory. Defaults to ./<name>.")]
        [CommandOption("-o|--output")]
        public string? Output { get; init; }

        [Description("Database provider: postgresql (default) or sqlserver.")]
        [CommandOption("--db")]
        public string? Database { get; init; }

        [Description("Exclude the .NET Aspire AppHost project.")]
        [CommandOption("--no-aspire")]
        [DefaultValue(false)]
        public bool NoAspire { get; init; }

        [Description("Skip interactive prompts and use defaults.")]
        [CommandOption("--non-interactive")]
        [DefaultValue(false)]
        public bool NonInteractive { get; init; }

        [Description("Initialize a git repository in the output directory.")]
        [CommandOption("--git")]
        [DefaultValue(true)]
        public bool InitGit { get; init; }

        [Description("Show what would be created without actually creating anything.")]
        [CommandOption("--dry-run")]
        [DefaultValue(false)]
        public bool DryRun { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        PrintBanner();

        // 1. Gather inputs (interactive or from arguments)
        string name = await ResolveNameAsync(settings, cancellationToken).ConfigureAwait(false);

        string? validationError = ProjectNameValidator.Validate(name);
        if (validationError is not null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]{validationError.EscapeMarkup()}[/]");
            return 1;
        }

        string db = await ResolveDatabaseAsync(settings, cancellationToken).ConfigureAwait(false);

        bool aspire = await ResolveAspireAsync(settings, cancellationToken).ConfigureAwait(false);

        string output = settings.Output ?? Path.GetFullPath(name);

        // 2. Check for existing directory
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Directory '{output}' already exists and is not empty.[/]");

            if (settings.NonInteractive)
            {
                AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Use --output to specify a different directory, or delete the existing one.[/]");
                return 1;
            }

            bool overwrite = await new ConfirmationPrompt("Overwrite existing directory?") { DefaultValue = false }
                .ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);

            if (!overwrite) return 1;
        }

        // 3. Print summary
        PrintSummary(name, db, aspire, output, settings.DryRun);

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Dry run — no files were created.[/]");
            return 0;
        }

        // 4. Ensure template is installed
        if (!await EnsureTemplateInstalledAsync(cancellationToken).ConfigureAwait(false))
            return 1;

        // 5. Scaffold project
        int result = await ScaffoldProjectAsync(name, db, aspire, output, cancellationToken).ConfigureAwait(false);
        if (result != 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Scaffolding failed. Check the output above for errors.[/]");
            return 1;
        }

        // 6. Initialize git
        if (settings.InitGit)
        {
            await InitGitRepoAsync(output, cancellationToken).ConfigureAwait(false);
        }

        // 7. Check for CLI updates (non-blocking, best-effort)
        await CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);

        // 8. Print next steps
        PrintNextSteps(name, aspire);

        return 0;
    }

    private static void PrintBanner()
    {
        AnsiConsole.Write(new FigletText("fsh").Color(Color.DodgerBlue1));
        AnsiConsole.MarkupLine("[bold]FullStackHero .NET Starter Kit[/]");
        AnsiConsole.WriteLine();
    }

    private static async Task<string> ResolveNameAsync(Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Name is not null) return settings.Name;

        if (settings.NonInteractive)
            throw new InvalidOperationException("Project name is required in non-interactive mode. Pass it as: fsh new <name>");

        return await new TextPrompt<string>($"[{FshConstants.AccentColor}]Project name:[/]")
            .Validate(input =>
            {
                string? error = ProjectNameValidator.Validate(input);
                return error is null
                    ? ValidationResult.Success()
                    : ValidationResult.Error(error);
            })
            .ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> ResolveDatabaseAsync(Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Database is not null) return settings.Database;
        if (settings.NonInteractive) return "postgresql";

        return await new SelectionPrompt<string>()
            .Title($"[{FshConstants.AccentColor}]Database provider:[/]")
            .AddChoices("postgresql", "sqlserver")
            .ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ResolveAspireAsync(Settings settings, CancellationToken cancellationToken)
    {
        if (settings.NoAspire) return false;
        if (settings.NonInteractive) return true;

        return await new ConfirmationPrompt($"[{FshConstants.AccentColor}]Include .NET Aspire AppHost?[/]")
            { DefaultValue = true }
            .ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);
    }

    private static void PrintSummary(string name, string db, bool aspire, string output, bool dryRun)
    {
        AnsiConsole.WriteLine();

        string mode = dryRun ? " [yellow](dry run)[/]" : "";
        AnsiConsole.MarkupLine($"[bold]Creating project:[/] {name.EscapeMarkup()}{mode}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Database:[/]  {db}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Aspire:[/]    {(aspire ? "yes" : "no")}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Output:[/]    {output.EscapeMarkup()}");
        AnsiConsole.WriteLine();
    }

    private static async Task<bool> EnsureTemplateInstalledAsync(CancellationToken cancellationToken)
    {
        // Check if the template is already available. dotnet new list may return
        // non-zero due to workload warnings, so check stdout content regardless.
        (_, string listOutput) = await ProcessRunner.CaptureAsync(
            "dotnet", $"new list {FshConstants.TemplateShortName}",
            cancellationToken).ConfigureAwait(false);

        bool installed = listOutput.Contains(FshConstants.TemplateShortName, StringComparison.OrdinalIgnoreCase)
            && listOutput.Contains("FullStackHero", StringComparison.OrdinalIgnoreCase);

        if (installed) return true;

        AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]FSH template not found. Installing...[/]");
        await ProcessRunner.RunAsync(
            "dotnet", $"new install {FshConstants.TemplatePackageId}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Verify it actually installed (ignore exit code — workload warnings cause non-zero)
        (_, string verifyOutput) = await ProcessRunner.CaptureAsync(
            "dotnet", $"new list {FshConstants.TemplateShortName}",
            cancellationToken).ConfigureAwait(false);

        bool nowInstalled = verifyOutput.Contains("FullStackHero", StringComparison.OrdinalIgnoreCase);
        if (!nowInstalled)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Failed to install template. Run manually:[/] dotnet new install {FshConstants.TemplatePackageId}");
        }

        return nowInstalled;
    }

    private static async Task<int> ScaffoldProjectAsync(
        string name, string db, bool aspire, string output, CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
            .StartAsync("Scaffolding project...", async _ =>
            {
                string aspireFlag = aspire ? "true" : "false";
                string args = $"new {FshConstants.TemplateShortName} -n {name} -o \"{output}\" --db {db} --aspire {aspireFlag} --force";
                await ProcessRunner.RunAsync("dotnet", args, showOutput: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                // dotnet new may return non-zero due to workload warnings even on success.
                // Verify by checking if the output directory was populated.
                string slnxPath = Path.Combine(output, "src", $"{name}.slnx");
                if (File.Exists(slnxPath))
                    return 0;

                // Fallback: check if any .slnx exists (template may have different naming)
                bool anySolution = Directory.Exists(Path.Combine(output, "src"))
                    && Directory.GetFiles(Path.Combine(output, "src"), "*.slnx").Length > 0;

                return anySolution ? 0 : 1;
            }).ConfigureAwait(false);
    }

    private static async Task InitGitRepoAsync(string output, CancellationToken cancellationToken)
    {
        if (Directory.Exists(Path.Combine(output, ".git")))
            return;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
            .StartAsync("Initializing git repository...", async _ =>
            {
                await ProcessRunner.RunAsync("git", "init", output, showOutput: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await ProcessRunner.RunAsync("git", "add -A", output, showOutput: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await ProcessRunner.RunAsync("git", "commit -m \"Initial project from FullStackHero .NET Starter Kit\"", output, showOutput: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    private static async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? latest = await NuGetClient.GetLatestVersionAsync(
                FshConstants.CliPackageId, cancellationToken).ConfigureAwait(false);

            string currentVersion = typeof(NewCommand).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

            if (latest is not null && latest != currentVersion)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]A newer version of FSH CLI is available: {latest} (current: {currentVersion})[/]");
                AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Run 'fsh update' to upgrade.[/]");
            }
        }
        catch
        {
            // Update check is best-effort — never block project creation
        }
    }

    private static void PrintNextSteps(string name, bool aspire)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[{FshConstants.SuccessColor}]Project created successfully![/]").RuleStyle(FshConstants.SuccessColor));
        AnsiConsole.WriteLine();

        string runProject = aspire
            ? $"src/Host/{name}.AppHost"
            : $"src/Host/{name}.Api";

        var tree = new Tree($"[bold {FshConstants.AccentColor}]Next Steps[/]");
        tree.AddNode($"[bold]cd[/] {name.EscapeMarkup()}");
        tree.AddNode($"[bold]dotnet run[/] --project {runProject.EscapeMarkup()}");

        if (aspire)
            tree.AddNode($"[{FshConstants.DimColor}]Aspire dashboard:[/] https://localhost:{FshConstants.AspireDashboardPort}");

        tree.AddNode($"[{FshConstants.DimColor}]API docs:[/]         https://localhost:{FshConstants.ApiHttpsPort}/scalar");
        tree.AddNode($"[{FshConstants.DimColor}]Health check:[/]     https://localhost:{FshConstants.ApiHttpsPort}/health");
        tree.AddNode($"[{FshConstants.DimColor}]Documentation:[/]    {FshConstants.DocsUrl}");

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }
}
