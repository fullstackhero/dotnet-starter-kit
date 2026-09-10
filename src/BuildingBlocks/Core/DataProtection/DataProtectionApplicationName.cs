using System.Reflection;

namespace FSH.Framework.Core.DataProtection;

/// <summary>
/// Resolves the Data Protection application name, which is what Data Protection isolates keys by.
/// </summary>
/// <remarks>
/// Read from configuration rather than hard-coded, because the framework also ships as compiled
/// NuGet packages where the template's token substitution cannot reach a literal. A literal would
/// make every project built on those packages share one key ring, and two such apps pointed at the
/// same store could decrypt each other's auth cookies and tokens. appsettings.json IS scaffolded
/// source, so the value there is renamed per project in both distribution modes.
/// </remarks>
public static class DataProtectionApplicationName
{
    /// <summary>Configuration key holding the application name.</summary>
    public const string ConfigurationKey = "DataProtection:ApplicationName";

    /// <summary>
    /// The configured value, else the entry assembly name.
    /// </summary>
    /// <remarks>
    /// Takes the already-read value rather than <c>IConfiguration</c> so Core keeps its
    /// deliberately minimal dependency set. The fallback errs towards isolation: a host that forgot
    /// the setting gets its own key ring rather than silently joining someone else's.
    /// </remarks>
    public static string Resolve(string? configuredValue) =>
        !string.IsNullOrWhiteSpace(configuredValue)
            ? configuredValue
            : Assembly.GetEntryAssembly()?.GetName().Name ?? "FSH.Starter";
}
