using Spectre.Console;

namespace FSH.CLI.Infrastructure;

/// <summary>
/// Installs the FSH <c>dotnet new</c> template, honouring local-path / version / source
/// overrides. Shared by <c>fsh new</c> and <c>fsh framework swap</c> so both resolve the
/// template the same way.
/// </summary>
internal static class TemplateInstaller
{
    /// <summary>
    /// Makes sure a template is installed, honouring any explicit source/version override.
    /// </summary>
    /// <remarks>
    /// Without an override this keeps the historical behaviour: any installed FSH template is
    /// accepted as-is. That is deliberately sticky, and it is also why the overrides exist —
    /// a contributor working on a fork, or anyone whose installed template has gone stale,
    /// otherwise has no way to make `fsh new` use anything else.
    ///
    /// Exit codes are ignored throughout because `dotnet new` returns non-zero for unrelated
    /// workload warnings; success is confirmed by re-listing the templates instead.
    /// </remarks>
    internal static async Task<bool> EnsureInstalledAsync(
        string? templatePath, string? templateVersion, string? templateSource, bool refresh,
        CancellationToken cancellationToken)
    {
        string? path = FromSettingOrEnvironment(templatePath, FshConstants.TemplatePathEnvVar);
        string? version = FromSettingOrEnvironment(templateVersion, FshConstants.TemplateVersionEnvVar);
        string? source = FromSettingOrEnvironment(templateSource, FshConstants.TemplateSourceEnvVar);
        bool overridden = path is not null || version is not null || source is not null || refresh;

        if (!overridden)
        {
            (_, string listOutput) = await ProcessRunner.CaptureAsync(
                "dotnet", $"new list {FshConstants.TemplateShortName}",
                cancellationToken).ConfigureAwait(false);

            bool installed = listOutput.Contains(FshConstants.TemplateShortName, StringComparison.OrdinalIgnoreCase)
                && listOutput.Contains("FullStackHero", StringComparison.OrdinalIgnoreCase);

            if (installed) return true;

            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]FSH template not found. Installing...[/]");
        }

        string target = FshConstants.TemplatePackageId;
        if (path is not null)
            target = $"\"{ResolveTemplatePath(path)}\"";
        else if (version is not null)
            target = $"{FshConstants.TemplatePackageId}::{version}";

        string arguments = $"new install {target} --force"
            + (source is not null ? $" --add-source \"{source}\"" : string.Empty);

        if (overridden)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Installing template: {target.EscapeMarkup()}[/]");

            // Uninstall first. Two packages that share the template identity
            // "FullStackHero.NET.StarterKit" make the template engine throw
            // ("Sequence contains more than one matching element") on the next `dotnet new fsh`,
            // and installing from a new source without removing the old one is precisely how
            // that state arises. Both forms are removed: the NuGet id, and the path we are
            // about to install (re-installing the same folder otherwise duplicates it).
            foreach (string uninstallTarget in (string[])[FshConstants.TemplatePackageId, target])
            {
                await ProcessRunner.CaptureAsync(
                    "dotnet", $"new uninstall {uninstallTarget}", cancellationToken).ConfigureAwait(false);
            }
        }

        await ProcessRunner.RunAsync("dotnet", arguments, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Verify by re-listing rather than trusting the exit code (see remarks).
        (_, string verifyOutput) = await ProcessRunner.CaptureAsync(
            "dotnet", $"new list {FshConstants.TemplateShortName}",
            cancellationToken).ConfigureAwait(false);

        bool nowInstalled = verifyOutput.Contains("FullStackHero", StringComparison.OrdinalIgnoreCase);
        if (!nowInstalled)
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Failed to install template. Run manually:[/] dotnet {arguments.EscapeMarkup()}");

        return nowInstalled;
    }

    /// <summary>
    /// Resolves what to hand <c>dotnet new install</c> for a local template path.
    /// </summary>
    /// <remarks>
    /// Accepts all three shapes people reasonably pass: a .nupkg file, a starter-kit checkout
    /// (identified by its .template.config), or a folder of packed nupkgs — in which case the
    /// newest matching package is chosen, since "-o ./nupkgs" is exactly where `dotnet pack`
    /// puts it and installing a bare folder of packages otherwise fails with a confusing
    /// "no templates found in package".
    /// </remarks>
    internal static string ResolveTemplatePath(string path)
    {
        string full = Path.GetFullPath(path);

        if (File.Exists(full))
            return full;

        if (Directory.Exists(full))
        {
            if (Directory.Exists(Path.Combine(full, ".template.config")))
                return full;

            string? newest = Directory
                .EnumerateFiles(full, $"{FshConstants.TemplatePackageId}*.nupkg")
                .MaxBy(File.GetLastWriteTimeUtc);

            if (newest is not null)
                return newest;
        }

        return full;
    }

    /// <summary>Setting, else environment variable, else <see langword="null"/>.</summary>
    internal static string? FromSettingOrEnvironment(string? value, string environmentVariable)
    {
        if (!string.IsNullOrWhiteSpace(value)) return value;

        string? fromEnvironment = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(fromEnvironment) ? null : fromEnvironment;
    }
}
