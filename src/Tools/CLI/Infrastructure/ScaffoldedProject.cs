using System.Text.RegularExpressions;
using Spectre.Console;

namespace FSH.CLI.Infrastructure;

/// <summary>
/// Reads and edits the few files that decide whether a scaffolded project consumes the FSH
/// kernel as source or as <c>FSH.Framework.*</c> packages.
/// </summary>
internal sealed partial class ScaffoldedProject
{
    private ScaffoldedProject(string root, string name, string solutionPath)
    {
        Root = root;
        Name = name;
        SolutionPath = solutionPath;
    }

    /// <summary>Absolute path to the project root (the directory containing <c>src</c>).</summary>
    internal string Root { get; }

    /// <summary>Project name, taken from the solution file name.</summary>
    internal string Name { get; }

    internal string SolutionPath { get; }

    internal string BuildingBlocksPath => Path.Combine(Root, "src", "BuildingBlocks");

    internal string FrameworkTestsPath => Path.Combine(Root, "src", "Tests", "Framework.Tests");

    internal string NuGetConfigPath => Path.Combine(Root, "NuGet.config");

    internal string PackagesPropsPath => Path.Combine(Root, "src", "Directory.Packages.props");

    /// <summary>True when the kernel is present as source, i.e. the project is not in package mode.</summary>
    internal bool HasBuildingBlocksSource => Directory.Exists(BuildingBlocksPath);

    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> looking for a scaffolded project,
    /// identified by exactly one <c>src/*.slnx</c>.
    /// </summary>
    internal static ScaffoldedProject? Locate(string? startDirectory = null)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startDirectory ?? Directory.GetCurrentDirectory()));

        while (directory is not null)
        {
            string src = Path.Combine(directory.FullName, "src");
            if (Directory.Exists(src))
            {
                string[] solutions = Directory.GetFiles(src, "*.slnx");
                if (solutions.Length == 1)
                {
                    return new ScaffoldedProject(
                        directory.FullName,
                        Path.GetFileNameWithoutExtension(solutions[0]),
                        solutions[0]);
                }
            }

            directory = directory.Parent;
        }

        return null;
    }

    // The <Folder Name="/BuildingBlocks/"> block, however it is indented or line-broken.
    [GeneratedRegex(@"[ \t]*<Folder Name=""/BuildingBlocks/"">.*?</Folder>\s*?\r?\n",
                    RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex BuildingBlocksFolder { get; }

    [GeneratedRegex(@"[ \t]*<Project Path=""Tests/Framework\.Tests/Framework\.Tests\.csproj""[^>]*/>\s*?\r?\n",
                    RegexOptions.ExplicitCapture)]
    private static partial Regex FrameworkTestsEntry { get; }

    /// <summary>Adds the kernel projects back into the solution, in source order.</summary>
    internal void AddKernelToSolution(IReadOnlyList<string> projects, bool includeFrameworkTests)
    {
        string solution = File.ReadAllText(SolutionPath);

        if (!BuildingBlocksFolder.IsMatch(solution))
        {
            string entries = string.Concat(projects
                .OrderBy(project => project, StringComparer.Ordinal)
                .Select(project => $"    <Project Path=\"BuildingBlocks/{project}/{project}.csproj\" />\n"));

            solution = solution.Replace(
                "<Solution>\n",
                $"<Solution>\n  <Folder Name=\"/BuildingBlocks/\">\n{entries}  </Folder>\n",
                StringComparison.Ordinal);
        }

        if (includeFrameworkTests && !FrameworkTestsEntry.IsMatch(solution))
        {
            solution = solution.Replace(
                "    <Project Path=\"Tests/Architecture.Tests/Architecture.Tests.csproj\" />\n",
                "    <Project Path=\"Tests/Architecture.Tests/Architecture.Tests.csproj\" />\n"
                + "    <Project Path=\"Tests/Framework.Tests/Framework.Tests.csproj\" />\n",
                StringComparison.Ordinal);
        }

        File.WriteAllText(SolutionPath, solution);
    }

    /// <summary>Removes the kernel projects from the solution.</summary>
    internal void RemoveKernelFromSolution()
    {
        string solution = File.ReadAllText(SolutionPath);
        solution = BuildingBlocksFolder.Replace(solution, string.Empty);
        solution = FrameworkTestsEntry.Replace(solution, string.Empty);
        File.WriteAllText(SolutionPath, solution);
    }

    /// <summary>
    /// Writes the NuGet.config that points the project at the feed serving its framework packages.
    /// </summary>
    internal void WriteNuGetConfig(string feed)
    {
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

        File.WriteAllText(NuGetConfigPath, content);
    }

    /// <summary>
    /// Drops the local framework feed from NuGet.config, removing the file outright when that
    /// was the only thing it added (which is the case for a CLI-generated one).
    /// </summary>
    internal bool RemoveLocalFeedSource()
    {
        if (!File.Exists(NuGetConfigPath)) return false;

        string content = File.ReadAllText(NuGetConfigPath);
        if (!content.Contains(FshConstants.LocalFeedSourceName, StringComparison.Ordinal))
            return false;

        File.Delete(NuGetConfigPath);
        return true;
    }

    /// <summary>Reads the currently pinned FSH.Framework.* version, if the project has one.</summary>
    internal string? GetFrameworkVersion()
    {
        if (!File.Exists(PackagesPropsPath)) return null;

        Match match = FrameworkVersionElement.Match(File.ReadAllText(PackagesPropsPath));
        if (!match.Success) return null;

        string value = match.Value;
        int start = value.IndexOf('>', StringComparison.Ordinal) + 1;
        int end = value.LastIndexOf('<');

        return end > start ? value[start..end] : null;
    }

    /// <summary>Pins the version of the FSH.Framework.* packages the project consumes.</summary>
    internal bool SetFrameworkVersion(string version)
    {
        if (!File.Exists(PackagesPropsPath)) return false;

        string content = File.ReadAllText(PackagesPropsPath);
        string updated = FrameworkVersionElement.Replace(
            content,
            $"<FshFrameworkVersion Condition=\"'$(FshFrameworkVersion)' == ''\">{version}</FshFrameworkVersion>",
            1);

        if (string.Equals(content, updated, StringComparison.Ordinal)) return false;

        File.WriteAllText(PackagesPropsPath, updated);
        return true;
    }

    [GeneratedRegex(@"<FshFrameworkVersion Condition=""'\$\(FshFrameworkVersion\)' == ''"">[^<]*</FshFrameworkVersion>",
                    RegexOptions.ExplicitCapture)]
    private static partial Regex FrameworkVersionElement { get; }

    /// <summary>Copies a directory tree, skipping build output.</summary>
    internal static void CopyTree(string source, string destination)
    {
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(directory)) continue;
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        Directory.CreateDirectory(destination);

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(file)) continue;

            string target = file.Replace(source, destination, StringComparison.Ordinal);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.EndsWith($"{Path.DirectorySeparatorChar}bin", StringComparison.Ordinal)
        || path.EndsWith($"{Path.DirectorySeparatorChar}obj", StringComparison.Ordinal);
}
