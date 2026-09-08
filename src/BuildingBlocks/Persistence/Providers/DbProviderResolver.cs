using FSH.Framework.Shared.Persistence;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// Maps EF Core's provider assembly name onto the framework's <see cref="DbProviders"/> constants.
/// </summary>
/// <remarks>
/// Resolving from <c>Database.ProviderName</c> rather than <c>DatabaseOptions.Provider</c> means the
/// conventions pass works identically at design time (<c>dotnet ef</c>), under hand-constructed test
/// contexts, and at runtime — none of which reliably have the options bound.
/// </remarks>
public static class DbProviderResolver
{
    private const string NpgsqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    /// <summary>
    /// Resolves an EF Core provider assembly name to a <see cref="DbProviders"/> constant, or null
    /// when the provider is not one the framework has provider-specific conventions for (SQLite and
    /// the in-memory provider used by tests both land here, and are treated as a no-op).
    /// </summary>
    public static string? FromEfProviderName(string? efProviderName) => efProviderName switch
    {
        NpgsqlProvider => DbProviders.PostgreSQL,
        SqlServerProvider => DbProviders.MSSQL,
        _ => null
    };

    /// <summary>
    /// Normalizes a configured provider string, returning null when it is not recognized.
    /// </summary>
    public static string? Normalize(string? provider) => provider?.ToUpperInvariant() switch
    {
        DbProviders.PostgreSQL => DbProviders.PostgreSQL,
        DbProviders.MSSQL => DbProviders.MSSQL,
        _ => null
    };
}
