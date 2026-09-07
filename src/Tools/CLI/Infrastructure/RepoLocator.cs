namespace FSH.CLI.Infrastructure;

/// <summary>
/// Locates the root of a FullStackHero starter-kit checkout.
/// </summary>
/// <remarks>
/// The <c>fsh framework</c> commands are maintainer commands: they build packages from
/// BuildingBlocks source and only make sense inside a starter-kit clone. Resolving the root
/// by walking up from the working directory (rather than assuming it) also contains the one
/// real hazard of shipping maintainer commands in a consumer tool — a globally installed
/// <c>fsh</c> being pointed at an unrelated directory.
/// </remarks>
internal static class RepoLocator
{
    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> looking for a starter-kit root, identified
    /// by <c>src/BuildingBlocks</c> and <c>.template.config</c> sitting side by side. Requiring
    /// both avoids matching an unrelated repository that happens to have one of them.
    /// </summary>
    /// <returns>The absolute repository root, or <see langword="null"/> if there is none.</returns>
    internal static string? FindStarterKitRoot(string? startDirectory = null)
    {
        DirectoryInfo? directory = new(startDirectory ?? Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "BuildingBlocks"))
                && Directory.Exists(Path.Combine(directory.FullName, ".template.config")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
