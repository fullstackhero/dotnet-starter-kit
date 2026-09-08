using System.Data.Common;
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
    /// SQL Server compatibility level the MSSQL provider targets: 170 (SQL Server 2025 / Azure SQL).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required for the native <c>json</c> column type the framework's portable JSON columns map to
    /// — <c>UseSqlServer</c> otherwise defaults to level 150 (SQL Server 2019), where EF Core emits
    /// <c>nvarchar(max)</c> instead and the generated migrations would no longer match the model.
    /// </para>
    /// <para>
    /// This is the reason MSSQL support requires SQL Server 2025 (17.x) or Azure SQL. Earlier
    /// versions have no <c>json</c> type and the migrations will not apply to them.
    /// </para>
    /// </remarks>
    private const int MssqlCompatibilityLevel = 170;

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

        ConfigureCommon(builder, isDevelopment);

        switch (dbProvider.ToUpperInvariant())
        {
            case DbProviders.PostgreSQL:
                builder.UseNpgsql(connectionString, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                });
                break;

            case DbProviders.MSSQL:
                // Deliberately no EnableRetryOnFailure: the retrying execution strategy refuses
                // user-initiated transactions, and the outbox joins the business transaction via
                // Database.UseTransactionAsync. Enabling it here breaks every transactional publish.
                builder.UseSqlServer(connectionString, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                    e.UseCompatibilityLevel(MssqlCompatibilityLevel);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Database Provider {dbProvider} is not supported.");
        }

        return builder;
    }

    /// <summary>
    /// Configures the provider against an existing <see cref="DbConnection"/> owned by the DI scope
    /// rather than a connection string.
    ///
    /// Every context in a scope sharing one connection object is what allows the outbox write to
    /// join the business transaction — EF Core can only enlist a context in an existing transaction
    /// when both contexts sit on the same connection.
    /// </summary>
    public static DbContextOptionsBuilder ConfigureHeroDatabase(
        this DbContextOptionsBuilder builder,
        string dbProvider,
        DbConnection connection,
        string migrationsAssembly,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(dbProvider);

        ConfigureCommon(builder, isDevelopment);

        switch (dbProvider.ToUpperInvariant())
        {
            case DbProviders.PostgreSQL:
                // contextOwnsConnection: false — the scope disposes it, not the first context to finish.
                builder.UseNpgsql(connection, contextOwnsConnection: false, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                });
                break;

            case DbProviders.MSSQL:
                // Deliberately no EnableRetryOnFailure: the retrying execution strategy refuses
                // user-initiated transactions, and the outbox joins the business transaction via
                // Database.UseTransactionAsync. Enabling it here breaks every transactional publish.
                builder.UseSqlServer(connection, contextOwnsConnection: false, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                    e.UseCompatibilityLevel(MssqlCompatibilityLevel);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Database Provider {dbProvider} is not supported.");
        }

        return builder;
    }

    private static void ConfigureCommon(DbContextOptionsBuilder builder, bool isDevelopment)
    {
        builder.ConfigureWarnings(warnings =>
            warnings.Log(RelationalEventId.PendingModelChangesWarning));

        if (isDevelopment)
        {
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        }
    }
}