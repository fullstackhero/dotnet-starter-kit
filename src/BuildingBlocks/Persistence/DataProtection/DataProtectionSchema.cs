using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Persistence.DataProtection;

/// <summary>
/// Creates the Data Protection key table before anything can read the key ring.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DataProtectionKeysDbInitializer"/> covers hosts that migrate before they serve — the
/// API and the integration-test harness. It is not enough for the DbMigrator, which builds and
/// STARTS a full host before running its own migration flow: Data Protection resolves its key ring
/// eagerly during that start, queries the table, and logs a failure with a stack trace on every
/// first run against an empty database. Nothing crashes, which is worse than crashing — a key
/// created while the table is missing cannot be persisted, so anything encrypted in that window is
/// undecryptable afterwards.
/// </para>
/// <para>
/// Deliberately standalone: it builds its own context rather than resolving one from the host,
/// because the whole point is to run before the host exists.
/// </para>
/// </remarks>
public static class DataProtectionSchema
{
    /// <summary>Applies pending migrations for the Data Protection key schema.</summary>
    public static async Task EnsureAsync(
        string dbProvider,
        string connectionString,
        string migrationsAssembly,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<DataProtectionKeysDbContext>();

        // ConfigureHeroDatabase rather than a bare UseNpgsql/UseSqlServer: it is the one call that
        // picks the provider, points at that provider's migrations assembly and applies the
        // provider-specific options, so this follows DatabaseOptions:Provider like every context.
        options.ConfigureHeroDatabase(dbProvider, connectionString, migrationsAssembly, isDevelopment);

        await using var context = new DataProtectionKeysDbContext(options.Options);

        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
