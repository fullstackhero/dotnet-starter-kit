using System.Xml.Linq;
using FSH.CLI.Models;

namespace FSH.CLI.Services;

/// <summary>
/// Service for updating package versions in a project.
/// </summary>
internal sealed class PackageUpdater
{
    /// <summary>
    /// Update Directory.Packages.props with new package versions.
    /// </summary>
    public static async Task<UpdateResult> UpdatePackagesPropsAsync(
        string projectPath,
        VersionDiff diff,
        UpdateOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new UpdateResult();
        var packagesPropsPath = FindPackagesPropsPath(projectPath);

        if (packagesPropsPath == null)
        {
            result.Errors.Add("Could not find Directory.Packages.props");
            return result;
        }

        if (options.DryRun)
        {
            // Just simulate what would happen
            return SimulateUpdate(diff, options);
        }

        try
        {
            // Read current file
            var content = await File.ReadAllTextAsync(packagesPropsPath, cancellationToken);
            var doc = XDocument.Parse(content);

            // Apply updates
            foreach (var update in diff.Updated)
            {
                if (options.SkipBreaking && update.IsBreaking)
                {
                    result.Skipped.Add($"{update.Package} (breaking change)");
                    continue;
                }

                var packageElement = doc.Descendants("PackageVersion")
                    .FirstOrDefault(e => string.Equals(
                        e.Attribute("Include")?.Value,
                        update.Package,
                        StringComparison.OrdinalIgnoreCase));

                if (packageElement != null)
                {
                    var versionAttr = packageElement.Attribute("Version");
                    if (versionAttr != null)
                    {
                        versionAttr.Value = update.ToVersion;
                        result.Updated.Add($"{update.Package}: {update.FromVersion} → {update.ToVersion}");
                    }
                }
            }

            // Add new packages
            var itemGroup = doc.Descendants("ItemGroup")
                .FirstOrDefault(ig => ig.Elements("PackageVersion").Any());

            if (itemGroup != null)
            {
                foreach (var added in diff.Added)
                {
                    // Check if package already exists
                    var exists = doc.Descendants("PackageVersion")
                        .Any(e => string.Equals(
                            e.Attribute("Include")?.Value,
                            added.Package,
                            StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        var newElement = new XElement("PackageVersion",
                            new XAttribute("Include", added.Package),
                            new XAttribute("Version", added.Version));
                        itemGroup.Add(newElement);
                        result.Added.Add($"{added.Package} ({added.Version})");
                    }
                }
            }

            // Note: We don't automatically remove packages as that could break the project
            foreach (var removed in diff.Removed)
            {
                result.Warnings.Add($"Package {removed.Package} is no longer in the latest release. Consider removing it manually if not needed.");
            }

            // Save the updated file
            await File.WriteAllTextAsync(packagesPropsPath, doc.ToString(), cancellationToken);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to update packages: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Update the FSH manifest after a successful upgrade.
    /// </summary>
    public static async Task<bool> UpdateManifestAsync(
        string projectPath,
        string newVersion,
        CancellationToken cancellationToken = default)
    {
        var manifest = FshManifest.TryLoad(projectPath);
        if (manifest == null)
            return false;

        manifest.FshVersion = newVersion;
        manifest.LastUpgradeAt = DateTimeOffset.UtcNow;

        // Update building blocks versions
        foreach (var key in manifest.Tracking.BuildingBlocks.Keys.ToList())
        {
            manifest.Tracking.BuildingBlocks[key] = newVersion;
        }

        try
        {
            manifest.Save(projectPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a backup of Directory.Packages.props before updating.
    /// </summary>
    public static async Task<string?> CreateBackupAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var packagesPropsPath = FindPackagesPropsPath(projectPath);
        if (packagesPropsPath == null)
            return null;

        var backupPath = packagesPropsPath + $".backup.{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            await File.WriteAllTextAsync(
                backupPath,
                await File.ReadAllTextAsync(packagesPropsPath, cancellationToken),
                cancellationToken);
            return backupPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Restore from a backup file.
    /// </summary>
    public static async Task<bool> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupPath))
            return false;

        var originalPath = backupPath.Split(".backup.")[0];

        try
        {
            await File.WriteAllTextAsync(
                originalPath,
                await File.ReadAllTextAsync(backupPath, cancellationToken),
                cancellationToken);
            File.Delete(backupPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindPackagesPropsPath(string projectPath)
    {
        var srcPath = Path.Combine(projectPath, "src", "Directory.Packages.props");
        if (File.Exists(srcPath))
            return srcPath;

        var rootPath = Path.Combine(projectPath, "Directory.Packages.props");
        if (File.Exists(rootPath))
            return rootPath;

        return null;
    }

    private static UpdateResult SimulateUpdate(VersionDiff diff, UpdateOptions options)
    {
        var result = new UpdateResult { Success = true, IsDryRun = true };

        foreach (var update in diff.Updated)
        {
            if (options.SkipBreaking && update.IsBreaking)
            {
                result.Skipped.Add($"{update.Package} (breaking change)");
            }
            else
            {
                result.Updated.Add($"{update.Package}: {update.FromVersion} → {update.ToVersion}");
            }
        }

        foreach (var added in diff.Added)
        {
            result.Added.Add($"{added.Package} ({added.Version})");
        }

        foreach (var removed in diff.Removed)
        {
            result.Warnings.Add($"Package {removed.Package} would need manual removal if not needed.");
        }

        return result;
    }
}

/// <summary>
/// Options for the package update operation.
/// </summary>
internal sealed class UpdateOptions
{
    public bool DryRun { get; set; }
    public bool SkipBreaking { get; set; }
    public bool Force { get; set; }
}

/// <summary>
/// Result of a package update operation.
/// </summary>
internal sealed class UpdateResult
{
    public bool Success { get; set; }
    public bool IsDryRun { get; set; }
    public List<string> Updated { get; } = [];
    public List<string> Added { get; } = [];
    public List<string> Skipped { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];

    public bool HasChanges => Updated.Count > 0 || Added.Count > 0;
}
