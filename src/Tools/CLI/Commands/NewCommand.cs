using System.ComponentModel;
using System.Reflection;
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

        [Description("Install the template from a local directory or .nupkg instead of NuGet. Env: FSH_TEMPLATE_PATH.")]
        [CommandOption("--template-path")]
        public string? TemplatePath { get; init; }

        [Description("Install a specific template version from NuGet. Env: FSH_TEMPLATE_VERSION.")]
        [CommandOption("--template-version")]
        public string? TemplateVersion { get; init; }

        [Description("Additional NuGet source to install the template from. Env: FSH_TEMPLATE_SOURCE.")]
        [CommandOption("--template-source")]
        public string? TemplateSource { get; init; }

        [Description("Re-install the template even if one is already installed.")]
        [CommandOption("--refresh-template")]
        [DefaultValue(false)]
        public bool RefreshTemplate { get; init; }

        [Description("Include the .agents AI rules/skills kit and AGENTS.md. Env: FSH_AGENTS=1.")]
        [CommandOption("--agents [VALUE]")]
        public FlagValue<bool?> Agents { get; init; } = new();

        [Description("Consume BuildingBlocks as FSH.Framework.* NuGet packages instead of scaffolding their source.")]
        [CommandOption("--framework-packages [VALUE]")]
        public FlagValue<bool?> FrameworkPackages { get; init; } = new();

        /// <summary>
        /// True when the flag was passed, either bare (<c>--framework-packages</c>) or with an
        /// explicit value (<c>--framework-packages true</c>).
        /// </summary>
        internal bool WantsFrameworkPackages => IsFlagSet(FrameworkPackages);

        /// <summary>
        /// True when the flag was passed and not explicitly negated.
        /// </summary>
        /// <remarks>
        /// The underlying value is <c>bool?</c>, not <c>bool</c>, on purpose: Spectre leaves the
        /// value at its default when a flag is passed bare, so with <c>bool</c> a plain
        /// <c>--agents</c> would be indistinguishable from <c>--agents false</c>. With
        /// <c>bool?</c>, bare means null, which reads as "yes".
        /// </remarks>
        internal static bool IsFlagSet(FlagValue<bool?> flag) => flag is { IsSet: true } && (flag.Value ?? true);

        [Description("Version of the FSH.Framework.* packages to pin. Defaults to the newest in the feed.")]
        [CommandOption("--framework-version")]
        public string? FrameworkVersion { get; init; }

        [Description("Local NuGet feed serving the FSH.Framework.* packages. Env: FSH_LOCAL_FEED.")]
        [CommandOption("--framework-feed")]
        public string? FrameworkFeedPath { get; init; }
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

        bool agents = await ResolveAgentsAsync(settings, cancellationToken).ConfigureAwait(false);

        // Framework packaging is opt-in and never prompted for: it is a deliberate, project-wide
        // architecture choice, not a per-scaffold convenience.
        string? frameworkFeed = settings.WantsFrameworkPackages ? FrameworkFeed.Resolve(settings.FrameworkFeedPath) : null;
        string? frameworkVersion = settings.WantsFrameworkPackages
            ? settings.FrameworkVersion
              ?? FrameworkFeed.GetLatestVersion(frameworkFeed!)
              ?? "0.0.0-local"
            : null;

        if (settings.WantsFrameworkPackages && !ValidateFrameworkFeed(frameworkFeed!, settings.FrameworkVersion))
            return 1;

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
        PrintSummary(name, aspire, frontend, agents, output, frameworkVersion, frameworkFeed, settings.DryRun);

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Dry run — no files were created.[/]");
            return 0;
        }

        // 4. Ensure template is installed
        if (!await TemplateInstaller.EnsureInstalledAsync(
                settings.TemplatePath, settings.TemplateVersion, settings.TemplateSource,
                settings.RefreshTemplate, cancellationToken).ConfigureAwait(false))
        {
            return 1;
        }

        // 5. Scaffold project
        int result = await ScaffoldProjectAsync(
            name, aspire, frontend, agents, frameworkVersion, output, cancellationToken).ConfigureAwait(false);
        if (result != 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Scaffolding failed. Check the output above for errors.[/]");
            return 1;
        }

        // 6. Generate per-project dev secrets + a ready-to-run docker-compose .env
        GenerateDevSecrets(name, output);
        bool dockerEnvReady = GenerateDockerEnv(output);

        // 6b. Point the project at the feed serving its framework packages.
        if (frameworkFeed is not null)
            GenerateNuGetConfig(output, frameworkFeed);

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
        PrintNextSteps(name, aspire, frontend, settings.SkipInstall, dockerEnvReady, frameworkFeed);

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

    private static async Task<bool> ResolveAgentsAsync(Settings settings, CancellationToken cancellationToken)
    {
        if (Settings.IsFlagSet(settings.Agents)) return true;

        string? fromEnvironment = Environment.GetEnvironmentVariable(FshConstants.AgentsEnvVar);
        if (fromEnvironment is "1" or "true" or "TRUE" or "True") return true;

        if (settings.NonInteractive) return false;

        return await new ConfirmationPrompt($"[{FshConstants.AccentColor}]Include the .agents AI rules kit (AGENTS.md + rules/skills)?[/]")
            { DefaultValue = false }
            .ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Warns early when framework packaging is requested but the feed cannot serve it — a
    /// scaffold that cannot restore is far more confusing than a message here.
    /// </summary>
    private static bool ValidateFrameworkFeed(string feed, string? explicitVersion)
    {
        if (!Directory.Exists(feed))
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Framework feed not found:[/] {feed.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Build the packages first, from a starter-kit clone: fsh framework pack --push[/]");
            return false;
        }

        if (explicitVersion is null && FrameworkFeed.GetLatestVersion(feed) is null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]No {FshConstants.FrameworkPackagePrefix}* packages in[/] {feed.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Build them first, from a starter-kit clone: fsh framework pack --push[/]");
            return false;
        }

        return true;
    }

    private static void PrintSummary(
        string name, bool aspire, bool frontend, bool agents, string output,
        string? frameworkVersion, string? frameworkFeed, bool dryRun)
    {
        AnsiConsole.WriteLine();

        string mode = dryRun ? " [yellow](dry run)[/]" : "";
        AnsiConsole.MarkupLine($"[bold]Creating project:[/] {name.EscapeMarkup()}{mode}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Aspire:[/]    {(aspire ? "yes" : "no")}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Frontend:[/]  {(frontend ? "yes (admin + dashboard)" : "no")}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Agents:[/]    {(agents ? "yes (.agents + AGENTS.md)" : "no")}");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Framework:[/] {(frameworkVersion is null
            ? "owned source (src/BuildingBlocks)"
            : $"packages {frameworkVersion.EscapeMarkup()}")}");

        if (frameworkFeed is not null)
            AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Feed:[/]      {frameworkFeed.EscapeMarkup()}");

        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]Output:[/]    {output.EscapeMarkup()}");
        AnsiConsole.WriteLine();
    }

    private static async Task<int> ScaffoldProjectAsync(
        string name, bool aspire, bool frontend, bool agents, string? frameworkVersion,
        string output, CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
            .StartAsync("Scaffolding project...", async _ =>
            {
                string aspireFlag = aspire ? "true" : "false";
                string frontendFlag = frontend ? "true" : "false";
                string agentsFlag = agents ? "true" : "false";
                string args =
                    $"new {FshConstants.TemplateShortName} -n \"{name}\" -o \"{output}\" " +
                    $"--aspire {aspireFlag} --frontend {frontendFlag} --agents {agentsFlag}" +
                    (frameworkVersion is not null
                        ? $" --frameworkPackages true --frameworkVersion {frameworkVersion}"
                        : string.Empty) +
                    " --force";

                // Named, not deconstructed with a discard: the enclosing status lambda already
                // binds "_" to its StatusContext.
                var scaffold = await ProcessRunner
                    .CaptureWithErrorAsync("dotnet", args, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                // dotnet new may return non-zero due to workload warnings even on success.
                // Verify by checking if the output directory was populated.
                string slnxPath = Path.Combine(output, "src", $"{name}.slnx");
                if (File.Exists(slnxPath))
                    return 0;

                // Fallback: check if any .slnx exists (template may have different naming)
                bool anySolution = Directory.Exists(Path.Combine(output, "src"))
                    && Directory.GetFiles(Path.Combine(output, "src"), "*.slnx").Length > 0;

                if (anySolution) return 0;

                // Genuinely failed — show what dotnet new said. Swallowing this leaves the user
                // with a bare "Scaffolding failed" and no way to find out why.
                IEnumerable<string> diagnostics = $"{scaffold.output}\n{scaffold.error}"
                    .Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line));

                foreach (string line in diagnostics)
                    AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]{line.TrimEnd().EscapeMarkup()}[/]");

                return 1;
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
                // Force the initial branch to main on every git version via symbolic-ref on the
                // unborn HEAD, regardless of the user's configured default-branch setting.
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

    // Generate deploy/docker/.env from .env.example with strong random secrets so compose
    // works without hand-filling 8 secrets; .env is git-ignored so the initial commit skips them.
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

    /// <summary>
    /// Writes a NuGet.config pointing the scaffolded project at the feed serving its
    /// FSH.Framework.* packages.
    /// </summary>
    /// <remarks>
    /// Written here rather than shipped in the template for two reasons: the feed path is only
    /// known at scaffold time, and a NuGet.config living at the starter kit's own root would
    /// hijack restore for the kit itself. Same post-scaffold approach as the docker .env.
    /// </remarks>
    private static void GenerateNuGetConfig(string output, string feed)
    {
        string path = Path.Combine(output, "NuGet.config");
        if (File.Exists(path)) return;

        // <clear /> so an inherited machine-level config cannot shadow the local feed.
        string content = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                <add key="{FshConstants.LocalFeedSourceName}" value="{feed}" />
              </packageSources>
            </configuration>

            """;

        try
        {
            File.WriteAllText(path, content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Could not write NuGet.config: {ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Add the feed manually: dotnet nuget add source \"{feed.EscapeMarkup()}\"[/]");
        }
    }

    private static async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? latest = await NuGetClient.GetLatestVersionAsync(
                FshConstants.CliPackageId, cancellationToken).ConfigureAwait(false);

            // Use the informational version (CI injects the real package version there), not
            // AssemblyVersion, which is pinned to 10.0.0.0 in the csproj and would make every
            // patch build nag about an "update" to itself.
            string currentVersion = typeof(NewCommand).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?.Split('+')[0]
                ?? typeof(NewCommand).Assembly.GetName().Version?.ToString(3)
                ?? "0.0.0";

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

    private static void PrintNextSteps(
        string name, bool aspire, bool frontend, bool skipInstall, bool dockerEnvReady, string? frameworkFeed)
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

        if (frameworkFeed is not null)
        {
            tree.AddNode($"[{FshConstants.DimColor}]Framework feed:[/]   {frameworkFeed.EscapeMarkup()}  [{FshConstants.DimColor}](see NuGet.config)[/]");
            // Everyone hits this once: without it the debugger silently steps over framework code.
            tree.AddNode($"[{FshConstants.DimColor}]To step into framework code, turn OFF \"Just My Code\" in your debugger.[/]");
        }

        tree.AddNode($"[{FshConstants.DimColor}]Documentation:[/]    {FshConstants.DocsUrl}");

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }
}
