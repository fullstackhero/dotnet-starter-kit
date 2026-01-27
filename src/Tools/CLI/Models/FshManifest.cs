using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSH.CLI.Models;

/// <summary>
/// Manifest file stored in .fsh/manifest.json to track project configuration and versions.
/// Used by the upgrade system to detect changes and apply updates.
/// </summary>
internal sealed class FshManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Version of the FullStackHero framework packages used.
    /// </summary>
    [JsonPropertyName("fshVersion")]
    public required string FshVersion { get; set; }

    /// <summary>
    /// Timestamp when the project was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Version of the CLI that created this project.
    /// </summary>
    [JsonPropertyName("cliVersion")]
    public required string CliVersion { get; set; }

    /// <summary>
    /// Project configuration options used during scaffolding.
    /// </summary>
    [JsonPropertyName("options")]
    public required FshManifestOptions Options { get; set; }

    /// <summary>
    /// Version tracking for building blocks and customizations.
    /// </summary>
    [JsonPropertyName("tracking")]
    public required FshManifestTracking Tracking { get; set; }

    /// <summary>
    /// Timestamp of the last upgrade applied.
    /// </summary>
    [JsonPropertyName("lastUpgradeAt")]
    public DateTimeOffset? LastUpgradeAt { get; set; }

    /// <summary>
    /// Serialize the manifest to JSON.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Deserialize a manifest from JSON.
    /// </summary>
    public static FshManifest? FromJson(string json) => JsonSerializer.Deserialize<FshManifest>(json, JsonOptions);

    /// <summary>
    /// Try to load a manifest from a project directory.
    /// </summary>
    public static FshManifest? TryLoad(string projectPath)
    {
        var manifestPath = Path.Combine(projectPath, ".fsh", "manifest.json");
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var json = File.ReadAllText(manifestPath);
            return FromJson(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Save the manifest to a project directory.
    /// </summary>
    public void Save(string projectPath)
    {
        var fshDir = Path.Combine(projectPath, ".fsh");
        Directory.CreateDirectory(fshDir);
        File.WriteAllText(Path.Combine(fshDir, "manifest.json"), ToJson());
    }
}

/// <summary>
/// Project configuration options stored in the manifest.
/// </summary>
internal sealed class FshManifestOptions
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("architecture")]
    public required string Architecture { get; set; }

    [JsonPropertyName("database")]
    public required string Database { get; set; }

    [JsonPropertyName("modules")]
    public required List<string> Modules { get; set; }

    [JsonPropertyName("includeDocker")]
    public bool IncludeDocker { get; set; }

    [JsonPropertyName("includeAspire")]
    public bool IncludeAspire { get; set; }

    [JsonPropertyName("includeTerraform")]
    public bool IncludeTerraform { get; set; }

    [JsonPropertyName("includeGitHubActions")]
    public bool IncludeGitHubActions { get; set; }
}

/// <summary>
/// Version tracking information for upgrades.
/// </summary>
internal sealed class FshManifestTracking
{
    /// <summary>
    /// Versions of each building block package.
    /// </summary>
    [JsonPropertyName("buildingBlocks")]
    public required Dictionary<string, string> BuildingBlocks { get; set; }

    /// <summary>
    /// List of files that have been customized by the user (detected via hash comparison).
    /// These files will be skipped or handled specially during upgrades.
    /// </summary>
    [JsonPropertyName("customizations")]
    public List<string> Customizations { get; set; } = [];
}
