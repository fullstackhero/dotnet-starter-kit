using System.ComponentModel;
using System.Security.Cryptography;
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

        [Description("Exclude the .NET Aspire AppHost project.")]
        [CommandOption("--no-aspire")]
        [DefaultValue(false)]
        public bool NoAspire { get; init; }

        [Description("Exclude the React admin + dashboard client apps.")]
        [CommandOption("--no-frontend")]
        [DefaultValue(false)]
        public bool NoFrontend { get; init; }

        [Description("Skip 'npm install' for the React apps after scaffolding.")]
        [CommandOption("--skip-install")]
        [DefaultValue(false)]
        public bool SkipInstall { get; init; }

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

        bool aspire = await ResolveAspireAsync(settings, cancellationToken).ConfigureAwait(false);

        bool frontend = await ResolveFrontendAsync(settings, cancellationToken).ConfigureAwait(false);

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
        PrintSummary(name, aspire, frontend, output, settings.DryRun);

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Dry run — no files were created.[/]");
            return 0;
        }

        // 4. Ensure template is installed
        if (!await EnsureTemplateInstalledAsync(cancellationToken).ConfigureAwait(false))
            return 1;

        // 5. Scaffold project
        int result = await ScaffoldProjectAsync(name, aspire, frontend, output, cancellationToken).ConfigureAwait(false);
        if (result != 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Scaffolding failed. Check the output above for errors.[/]");
            return 1;
        }

        // 6. Generate per-project dev secrets + a ready-to-run docker-compose .env
        GenerateDevSecrets(name, output);
        bool dockerEnvReady = GenerateDockerEnv(output);

        // 7. Install frontend dependencies (npm install in both React apps)
        if (frontend && !settings.SkipInstall)
        {
            await InstallFrontendAsync(output, cancellationToken).ConfigureAwait(false);
        }

        // 8. Initialize git (after files settle, so the initial commit is complete)
        if (settings.InitGit)
        {
            await InitGitRepoAsync(output, cancellationToken).ConfigureAwait(false);
        }

        // 9. Check for CLI updates (non-blocking, best-effort)
        await CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);

        // 10. Print next steps
        PrintNextSteps(name, aspire, frontend, settings.SkipInstall, dockerEnvReady);

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

    private static async Task<bool> ResolveAspireAsync(Settings settings, CancellationToken cancellationToken)
    {
        if (settings.NoAspire) return false;
        if (settings.NonInteractive) return true;

        return await new ConfirmationPrompt($"[{FshConstants.AccentColor}]Include .NET Aspire AppHost?[/]")
            { DefaultValue = true }
            .ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ResolveFrontendAsync(Settings settings, CancellationToken cancellationToken)
    {
        if (settings.NoFrontend) return false;
        if (settings.NonInteractive) return true;

        return await new ConfirmationPrompt($"[{FshConstants.AccentColor}]Include the React admin + dashboard apps?[/]")
            { DefaultValue = true }
            .ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);
    }

    private static void PrintSummary(string name, bool aspire, bool frontend, string output, bool dryRun)
    {
        AnsiConsole.WriteLine();

        string mode = dryRun ? " [yellow](dry run)[/]" : "";
        AnsiConsole.MarkupLine($"[bold]Creating project:[/] {name.EscapeMarkup()}{mode}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Aspire:[/]    {(aspire ? "yes" : "no")}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Frontend:[/]  {(frontend ? "yes (admin + dashboard)" : "no")}");
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
        string name, bool aspire, bool frontend, string output, CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
            .StartAsync("Scaffolding project...", async _ =>
            {
                string aspireFlag = aspire ? "true" : "false";
                string frontendFlag = frontend ? "true" : "false";
                string args = $"new {FshConstants.TemplateShortName} -n {name} -o \"{output}\" --aspire {aspireFlag} --frontend {frontendFlag} --force";
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
                // Default the initial branch to `main` regardless of the user's git
                // `init.defaultBranch` (older git defaults to `master`). symbolic-ref on
                // the unborn HEAD works on every git version, unlike `git init -b`.
                await ProcessRunner.RunAsync("git", "symbolic-ref HEAD refs/heads/main", output, showOutput: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await ProcessRunner.RunAsync("git", "add -A", output, showOutput: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await ProcessRunner.RunAsync("git", "commit -m \"Initial project from FullStackHero .NET Starter Kit\"", output, showOutput: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    private static async Task InstallFrontendAsync(string output, CancellationToken cancellationToken)
    {
        foreach (string app in (string[])["admin", "dashboard"])
        {
            string appDir = Path.Combine(output, "clients", app);
            if (!Directory.Exists(appDir)) continue;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
                .StartAsync($"Installing {app} dependencies (npm install)...", async _ =>
                {
                    try
                    {
                        int code = await RunNpmAsync("install --no-audit --no-fund", appDir, cancellationToken)
                            .ConfigureAwait(false);
                        if (code != 0)
                            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]npm install failed for clients/{app}. Run it manually before 'npm run dev'.[/]");
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
                    {
                        AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]npm not found — skipped clients/{app}. Install Node.js, then run 'npm install' there.[/]");
                    }
                }).ConfigureAwait(false);
        }
    }

    // npm is a batch shim (npm.cmd) on Windows, so it can't be launched directly via
    // CreateProcess (UseShellExecute=false); route it through cmd.exe there.
    private static Task<int> RunNpmAsync(string args, string workingDirectory, CancellationToken cancellationToken)
    {
        return OperatingSystem.IsWindows()
            ? ProcessRunner.RunAsync("cmd.exe", $"/c npm {args}", workingDirectory, showOutput: false, cancellationToken: cancellationToken)
            : ProcessRunner.RunAsync("npm", args, workingDirectory, showOutput: false, cancellationToken: cancellationToken);
    }

    // Replace the shared dev signing-key placeholder with a unique per-project key so two
    // freshly scaffolded projects never mint interchangeable tokens in development.
    private static void GenerateDevSecrets(string name, string output)
    {
        string appsettingsDev = Path.Combine(output, "src", "Host", $"{name}.Api", "appsettings.Development.json");
        if (!File.Exists(appsettingsDev)) return;

        const string placeholder = "fsh-dev-only-do-not-use-in-prod-32+chars-min";
        string content = File.ReadAllText(appsettingsDev);
        if (!content.Contains(placeholder, StringComparison.Ordinal)) return;

        string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        File.WriteAllText(appsettingsDev, content.Replace(placeholder, key, StringComparison.Ordinal));
    }

    // Generate deploy/docker/.env from .env.example with strong random secrets so
    // `cd deploy/docker && docker compose up` works without hand-filling 8 secrets.
    // .env is git-ignored, so the initial commit never captures these values.
    private static bool GenerateDockerEnv(string output)
    {
        string dockerDir = Path.Combine(output, "deploy", "docker");
        string examplePath = Path.Combine(dockerDir, ".env.example");
        string envPath = Path.Combine(dockerDir, ".env");
        if (!File.Exists(examplePath) || File.Exists(envPath)) return false;

        var overrides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["JWT_SIGNING_KEY"] = GenerateSecret(48),
            ["SEED_ADMIN_PASSWORD"] = GenerateSecret(20),
            ["HANGFIRE_USERNAME"] = "admin",
            ["HANGFIRE_PASSWORD"] = GenerateSecret(20),
            ["POSTGRES_PASSWORD"] = GenerateSecret(24),
            ["REDIS_PASSWORD"] = GenerateSecret(24),
            ["MINIO_ROOT_USER"] = "minioadmin",
            ["MINIO_ROOT_PASSWORD"] = GenerateSecret(24),
            // Local-working defaults: the compose stack publishes these host ports.
            ["FSH_API_URL"] = "http://localhost:8080",
            ["FSH_ADMIN_URL"] = "http://localhost:8081",
            ["FSH_DASHBOARD_URL"] = "http://localhost:8082"
        };

        string[] lines = File.ReadAllLines(examplePath);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.TrimStart().StartsWith('#')) continue;
            int eq = line.IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0) continue;
            string key = line[..eq];
            if (overrides.TryGetValue(key, out string? value))
                lines[i] = $"{key}={value}";
        }

        File.WriteAllLines(envPath, lines);
        return true;
    }

    // Strong, connection-string-safe secret: guaranteed upper/lower/digit/special,
    // drawn from an unambiguous alphabet (no 0/O/1/l/I, no quotes or ; + / = chars).
    private static string GenerateSecret(int length)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digit = "23456789";
        const string special = "-_.!";
        string all = upper + lower + digit + special;

        var chars = new char[length];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digit[RandomNumberGenerator.GetInt32(digit.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];
        for (int i = 4; i < length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        // Fisher-Yates shuffle so the guaranteed-class chars aren't always first.
        for (int i = length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? latest = await NuGetClient.GetLatestVersionAsync(
                FshConstants.CliPackageId, cancellationToken).ConfigureAwait(false);

            string currentVersion = typeof(NewCommand).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

            if (VersionComparer.IsNewer(latest, currentVersion))
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

    private static void PrintNextSteps(string name, bool aspire, bool frontend, bool skipInstall, bool dockerEnvReady)
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
        {
            tree.AddNode($"[{FshConstants.DimColor}]Aspire dashboard:[/] https://localhost:{FshConstants.AspireDashboardPort}");
            if (frontend)
                tree.AddNode($"[{FshConstants.DimColor}]Aspire launches the admin + dashboard apps automatically.[/]");
        }
        else if (frontend)
        {
            tree.AddNode("[bold]cd[/] clients/admin && [bold]npm run dev[/]      [dim](→ http://localhost:5173)[/]");
            tree.AddNode("[bold]cd[/] clients/dashboard && [bold]npm run dev[/]  [dim](→ http://localhost:5174)[/]");
        }

        if (frontend && skipInstall)
            tree.AddNode($"[{FshConstants.WarningColor}]Run 'npm install' in clients/admin and clients/dashboard first.[/]");

        tree.AddNode($"[{FshConstants.DimColor}]API docs:[/]         https://localhost:{FshConstants.ApiHttpsPort}/scalar");
        tree.AddNode($"[{FshConstants.DimColor}]Health check:[/]     https://localhost:{FshConstants.ApiHttpsPort}/health/live");

        if (dockerEnvReady)
            tree.AddNode($"[{FshConstants.DimColor}]Self-host:[/]        cd deploy/docker && docker compose up -d --build  [{FshConstants.DimColor}](secrets pre-generated in .env)[/]");

        tree.AddNode($"[{FshConstants.DimColor}]Documentation:[/]    {FshConstants.DocsUrl}");

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }
}
