namespace FSH.Framework.Core.DataProtection;

/// <summary>
/// Where ASP.NET Core Data Protection keys are persisted, selected by
/// <c>DataProtection:Store</c> in configuration.
///
/// Lives in Core because both Caching (which owns the Redis store) and Web (which owns the
/// database store) have to agree on the value, and Caching cannot reference Persistence.
/// </summary>
public static class DataProtectionStores
{
    /// <summary>Configuration key selecting the store.</summary>
    public const string ConfigurationKey = "DataProtection:Store";

    /// <summary>Redis, when configured. The default, and what the framework has always used.</summary>
    public const string Redis = "REDIS";

    /// <summary>The application database, via the framework's Data Protection keys context.</summary>
    public const string Database = "DATABASE";

    /// <summary>True when configuration selects the database store.</summary>
    public static bool UsesDatabase(string? configuredValue) =>
        string.Equals(configuredValue?.Trim(), Database, StringComparison.OrdinalIgnoreCase);
}
