using System.ComponentModel;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

/// <summary>
/// Brings an already-generated project up to date with a newer version of the template.
/// </summary>
/// <remarks>
/// Works as a three-way merge rather than a re-scaffold. The pristine scaffold commit that
/// <c>fsh new</c> created is the common ancestor; a freshly generated scaffold of the same
/// project - same name, same options - is the new state. Committing that new state on a branch
/// rooted at the ancestor lets git do the merge, so local changes are preserved and genuine
/// collisions surface as ordinary conflicts instead of being silently overwritten.
///
/// Everything happens in a detached git worktree, so the caller's working directory is never
/// touched until they merge.
/// </remarks>
public sealed class UpgradeCommand : AsyncCommand<UpgradeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Project directory to upgrade. Defaults to the current directory.")]
        [CommandOption("--project <PATH>")]
        public string? Project { get; init; }

        [Description("Commit holding the pristine scaffold. Defaults to the one 'fsh new' created.")]
        [CommandOption("--from-scaffold <REF>")]
        public string? FromScaffold { get; init; }

        [Description("Branch to put the template update on. Defaults to fsh/template-upgrade.")]
        [CommandOption("-b|--branch <NAME>")]
        public string? Branch { get; init; }

        [Description("Template to upgrade to: a checkout, .nupkg, or folder of nupkgs. Env: FSH_TEMPLATE_PATH.")]
        [CommandOption("--template-path <PATH>")]
        public string? TemplatePath { get; init; }

        [Description("Template version to upgrade to. Env: FSH_TEMPLATE_VERSION.")]
        [CommandOption("--template-version <VERSION>")]
        public string? TemplateVersion { get; init; }

        [Description("Merge the update into the current branch instead of stopping at the branch.")]
        [CommandOption("--merge")]
        [DefaultValue(false)]
        public bool Merge { get; init; }

        [Description("Show what would be regenerated without creating a branch.")]
        [CommandOption("--dry-run")]
        [DefaultValue(false)]
        public bool DryRun { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ScaffoldedProject? project = ScaffoldedProject.Locate(settings.Project);
        if (project is null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]No scaffolded project found here.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Run this inside a project created by 'fsh new', or pass --project <path>.[/]");
            return 1;
        }

        if (!await GitRunner.IsRepositoryAsync(project.Root, cancellationToken).ConfigureAwait(false))
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]{project.Root.EscapeMarkup()} is not a git repository.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]The upgrade is a git merge against the original scaffold commit, so history is required.[/]");
            return 1;
        }

        // A dirty tree would make the merge result impossible to separate from local edits.
        if (!await GitRunner.IsCleanAsync(project.Root, cancellationToken).ConfigureAwait(false))
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Working tree has uncommitted changes.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Commit or stash them first: the upgrade lands as a merge and must start from a clean state.[/]");
            return 1;
        }

        string? baseline = settings.FromScaffold
            ?? await GitRunner.FindScaffoldCommitAsync(project.Root, cancellationToken).ConfigureAwait(false);

        if (baseline is null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Could not find the original scaffold commit.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Looked for a commit named \"{FshConstants.InitialCommitMessage}\".[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Point at it explicitly with --from-scaffold <ref>.[/]");
            return 1;
        }

        ScaffoldOptions options = await DetectScaffoldOptionsAsync(project, baseline, cancellationToken).ConfigureAwait(false);

        var summary = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        summary.AddColumn("[bold]Setting[/]");
        summary.AddColumn("[bold]Value[/]");
        summary.AddRow("Project", $"{project.Name.EscapeMarkup()}  [{FshConstants.DimColor}]({project.Root.EscapeMarkup()})[/]");
        summary.AddRow("Scaffold commit", $"[{FshConstants.AccentColor}]{baseline[..Math.Min(8, baseline.Length)]}[/]");
        summary.AddRow("Options", options.Describe());
        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Would regenerate the scaffold with these options and put the diff on a branch.[/]");
            return 0;
        }

        if (!await TemplateInstaller.EnsureInstalledAsync(
                settings.TemplatePath, settings.TemplateVersion, templateSource: null,
                refresh: true, cancellationToken).ConfigureAwait(false))
        {
            return 1;
        }

        string staging = Path.Combine(Path.GetTempPath(), $"fsh-upgrade-{Guid.NewGuid():N}");
        string worktree = Path.Combine(Path.GetTempPath(), $"fsh-worktree-{Guid.NewGuid():N}");
        string branch = settings.Branch ?? "fsh/template-upgrade";

        // Remembered so the caller's checkout can be put back no matter how this exits. A tool
        // that creates worktrees and branches in someone's repository must never leave them on a
        // branch they did not ask for, and "it shouldn't happen" is not a guarantee.
        string? originalBranch = await GitRunner.CurrentBranchAsync(project.Root, cancellationToken).ConfigureAwait(false);

        try
        {
            if (!await GenerateScaffoldAsync(project, options, staging, cancellationToken).ConfigureAwait(false))
                return 1;

            await ReconcileGeneratedFilesAsync(project, baseline, staging, cancellationToken).ConfigureAwait(false);

            return await CommitAndReportAsync(
                project, baseline, branch, staging, worktree, settings.Merge, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await GitRunner.RunAsync(project.Root, $"worktree remove --force \"{worktree}\"", cancellationToken).ConfigureAwait(false);
            TryDelete(staging);
            TryDelete(worktree);

            await RestoreBranchAsync(project.Root, originalBranch, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The options a scaffold was created with, recovered from the shape of the baseline tree
    /// rather than remembered state, so an upgrade cannot regenerate a differently-shaped project.
    /// </summary>
    private sealed record ScaffoldOptions(bool Aspire, bool Frontend, bool Agents, bool FrameworkPackages, string? FrameworkVersion)
    {
        internal string Describe()
        {
            List<string> parts =
            [
                Aspire ? "aspire" : "no aspire",
                Frontend ? "frontend" : "no frontend",
                Agents ? "agents" : "no agents",
                FrameworkPackages ? $"framework packages {FrameworkVersion}" : "owned source"
            ];

            return string.Join(", ", parts).EscapeMarkup();
        }
    }

    private static async Task<ScaffoldOptions> DetectScaffoldOptionsAsync(
        ScaffoldedProject project, string baseline, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> tree = await GitRunner.ListTreeAsync(project.Root, baseline, cancellationToken).ConfigureAwait(false);

        bool Has(string prefix) => tree.Any(path => path.StartsWith(prefix, StringComparison.Ordinal));

        return new ScaffoldOptions(
            Aspire: Has($"src/Host/{project.Name}.AppHost/"),
            Frontend: Has("clients/"),
            Agents: Has(".agents/"),
            FrameworkPackages: !Has("src/BuildingBlocks/"),
            FrameworkVersion: project.GetFrameworkVersion());
    }

    private static async Task<bool> GenerateScaffoldAsync(
        ScaffoldedProject project, ScaffoldOptions options, string staging, CancellationToken cancellationToken)
    {
        string arguments =
            $"new {FshConstants.TemplateShortName} -n \"{project.Name}\" -o \"{staging}\" " +
            $"--aspire {Flag(options.Aspire)} --frontend {Flag(options.Frontend)} --agents {Flag(options.Agents)} " +
            "--skipRestore true --force" +
            (options.FrameworkPackages && options.FrameworkVersion is not null
                ? $" --frameworkPackages true --frameworkVersion {options.FrameworkVersion}"
                : string.Empty);

        int result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
            .StartAsync("Regenerating the scaffold from the new template...", async _ =>
            {
                var scaffold = await ProcessRunner
                    .CaptureWithErrorAsync("dotnet", arguments, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (Directory.Exists(Path.Combine(staging, "src"))) return 0;

                foreach (string line in $"{scaffold.output}\n{scaffold.error}".Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                    AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]{line.TrimEnd().EscapeMarkup()}[/]");

                return 1;
            }).ConfigureAwait(false);

        if (result != 0)
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Could not regenerate the scaffold.[/]");

        return result == 0;

        static string Flag(bool value) => value ? "true" : "false";
    }

    /// <summary>
    /// Restores the files <c>fsh new</c> writes after the template runs.
    /// </summary>
    /// <remarks>
    /// These are committed in the baseline but are not template output, so a plain regeneration
    /// would show them as deletions or reversions: NuGet.config would be deleted, and the
    /// project's unique dev signing key would be replaced by the shared placeholder. Carrying the
    /// baseline values forward keeps the diff to genuine template changes.
    /// </remarks>
    private static async Task ReconcileGeneratedFilesAsync(
        ScaffoldedProject project, string baseline, string staging, CancellationToken cancellationToken)
    {
        string? nugetConfig = await GitRunner
            .ShowFileAsync(project.Root, baseline, "NuGet.config", cancellationToken).ConfigureAwait(false);

        if (nugetConfig is not null)
            await File.WriteAllTextAsync(Path.Combine(staging, "NuGet.config"), nugetConfig, cancellationToken).ConfigureAwait(false);

        foreach (string host in Directory.Exists(Path.Combine(staging, "src", "Host"))
                     ? Directory.GetDirectories(Path.Combine(staging, "src", "Host"))
                     : [])
        {
            string relative = $"src/Host/{Path.GetFileName(host)}/appsettings.Development.json";
            string generated = Path.Combine(staging, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(generated)) continue;

            string content = await File.ReadAllTextAsync(generated, cancellationToken).ConfigureAwait(false);
            if (!content.Contains(FshConstants.DevSigningKeyPlaceholder, StringComparison.Ordinal)) continue;

            string? original = await GitRunner
                .ShowFileAsync(project.Root, baseline, relative, cancellationToken).ConfigureAwait(false);

            if (original is null) continue;

            string? key = ExtractSigningKey(original);
            if (key is null) continue;

            await File.WriteAllTextAsync(
                generated,
                content.Replace(FshConstants.DevSigningKeyPlaceholder, key, StringComparison.Ordinal),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? ExtractSigningKey(string appsettings)
    {
        const string marker = "\"SigningKey\":";
        int start = appsettings.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return null;

        int open = appsettings.IndexOf('"', start + marker.Length);
        if (open < 0) return null;

        int close = appsettings.IndexOf('"', open + 1);
        return close > open ? appsettings[(open + 1)..close] : null;
    }

    private static async Task<int> CommitAndReportAsync(
        ScaffoldedProject project, string baseline, string branch, string staging, string worktree,
        bool merge, CancellationToken cancellationToken)
    {
        // A separate worktree rooted at the scaffold commit: the caller's checkout is untouched.
        (bool created, string worktreeOutput) = await GitRunner
            .RunAsync(project.Root, $"worktree add -B {branch} \"{worktree}\" {baseline}", cancellationToken)
            .ConfigureAwait(false);

        if (!created)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Could not create the upgrade worktree.[/]");
            AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]{worktreeOutput.Trim().EscapeMarkup()}[/]");
            return 1;
        }

        // The worktree is a pristine checkout of the scaffold, so replacing its contents wholesale
        // is safe - there is no build output or local state to preserve.
        foreach (string entry in Directory.EnumerateFileSystemEntries(worktree))
        {
            if (Path.GetFileName(entry) is ".git") continue;

            if (Directory.Exists(entry)) Directory.Delete(entry, recursive: true);
            else File.Delete(entry);
        }

        ScaffoldedProject.CopyTree(staging, worktree);

        await GitRunner.RunAsync(worktree, "add -A", cancellationToken).ConfigureAwait(false);

        (bool staged, string status) = await GitRunner
            .RunAsync(worktree, "status --porcelain", cancellationToken).ConfigureAwait(false);

        if (staged && string.IsNullOrWhiteSpace(status))
        {
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Already up to date[/] - the template produces the same output as your scaffold.");
            await GitRunner.RunAsync(project.Root, $"branch -D {branch}", cancellationToken).ConfigureAwait(false);
            return 0;
        }

        await GitRunner.RunAsync(
            worktree, $"commit -q -m \"chore: update {project.Name} to the latest FSH template\"", cancellationToken)
            .ConfigureAwait(false);

        (_, string stat) = await GitRunner
            .RunAsync(worktree, $"diff --stat {baseline} HEAD", cancellationToken).ConfigureAwait(false);

        AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Template changes committed on[/] [{FshConstants.AccentColor}]{branch.EscapeMarkup()}[/]");
        foreach (string line in stat.Split('\n').TakeLast(1).Where(l => !string.IsNullOrWhiteSpace(l)))
            AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]{line.Trim().EscapeMarkup()}[/]");

        AnsiConsole.WriteLine();

        if (!merge)
        {
            AnsiConsole.MarkupLine("Review it, then merge:");
            AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]git diff {baseline[..Math.Min(8, baseline.Length)]}..{branch.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]git merge {branch.EscapeMarkup()}[/]");
            return 0;
        }

        (bool merged, string mergeOutput) = await GitRunner
            .RunAsync(project.Root, $"merge --no-edit {branch}", cancellationToken).ConfigureAwait(false);

        foreach (string line in mergeOutput.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Take(6))
            AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]{line.Trim().EscapeMarkup()}[/]");

        if (merged)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Merged.[/] Build to confirm: dotnet build src/{project.Name.EscapeMarkup()}.slnx");
            return 0;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Merge stopped on conflicts.[/] Resolve them, then 'git commit'.");
        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]To back out entirely: git merge --abort[/]");
        return 1;
    }

    /// <summary>
    /// Puts the repository back on the branch it started on, if anything moved it.
    /// </summary>
    /// <remarks>
    /// A successful <c>--merge</c> already ends on the original branch, so this is a no-op there.
    /// </remarks>
    private static async Task RestoreBranchAsync(
        string repository, string? originalBranch, CancellationToken cancellationToken)
    {
        if (originalBranch is null or "HEAD") return;

        string? current = await GitRunner.CurrentBranchAsync(repository, cancellationToken).ConfigureAwait(false);
        if (current is null || string.Equals(current, originalBranch, StringComparison.Ordinal)) return;

        (bool ok, _) = await GitRunner
            .RunAsync(repository, $"checkout {originalBranch}", cancellationToken).ConfigureAwait(false);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(ok
            ? $"[{FshConstants.DimColor}]Restored the checkout to '{originalBranch.EscapeMarkup()}'.[/]"
            : $"[{FshConstants.WarningColor}]Left on '{current.EscapeMarkup()}'; expected '{originalBranch.EscapeMarkup()}'. Run: git checkout {originalBranch.EscapeMarkup()}[/]");
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing the command over.
        }
    }
}
