using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSH.Framework.Persistence;

/// <summary>
/// Extension methods for configuring Entity Framework DbContextOptionsBuilder.
/// </summary>
public static class OptionsBuilderExtensions
{
    /// <summary>
    /// Configures the database provider and connection for the Hero framework.
    /// </summary>
    /// <param name="builder">The DbContextOptionsBuilder to configure.</param>
    /// <param name="dbProvider">The database provider (PostgreSQL, MSSQL).</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="migrationsAssembly">The assembly containing database migrations.</param>
    /// <param name="isDevelopment">Whether the application is running in development mode.</param>
    /// <returns>The configured DbContextOptionsBuilder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null or dbProvider is null/whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported database provider is specified.</exception>
    public static DbContextOptionsBuilder ConfigureHeroDatabase(
        this DbContextOptionsBuilder builder,
        string dbProvider,
        string connectionString,
        string migrationsAssembly,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(dbProvider);

        builder.ConfigureWarnings(warnings =>
            warnings.Log(RelationalEventId.PendingModelChangesWarning));

        switch (dbProvider.ToUpperInvariant())
        {
            case DbProviders.PostgreSQL:
                builder.UseNpgsql(connectionString, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                });
                break;

            case DbProviders.MSSQL:
                builder.UseSqlServer(connectionString, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                    e.EnableRetryOnFailure();
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Database Provider {dbProvider} is not supported.");
        }

        if (isDevelopment)
        {
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        }

        return builder;
    }
}