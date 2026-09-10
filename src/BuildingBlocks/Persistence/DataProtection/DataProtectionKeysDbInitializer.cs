using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Persistence.DataProtection;

/// <summary>
/// Migrates the Data Protection key schema.
/// </summary>
/// <remarks>
/// Registered as an <see cref="IDbInitializer"/> so the table is created by whatever brings the
/// schema up — the API host, the DbMigrator, and the integration-test harness alike. Wiring the
/// context without an initializer is the failure mode this exists to prevent: the table then only
/// appears wherever someone remembered to migrate it by hand, and every flow that protects a
/// payload (registration, password reset, two-factor) fails with a
/// <c>CryptographicException</c> everywhere else.
///
/// The context is not tenant-aware, so this targets the root connection on every pass; after the
/// first it is a no-op, which is why running inside the per-tenant loop is harmless.
/// </remarks>
public sealed partial class DataProtectionKeysDbInitializer : IDbInitializer
{
    private readonly DataProtectionKeysDbContext _context;
    private readonly ILogger<DataProtectionKeysDbInitializer> _logger;

    public DataProtectionKeysDbInitializer(
        DataProtectionKeysDbContext context,
        ILogger<DataProtectionKeysDbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await _context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            LogMigrated();
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "applied database migrations for the Data Protection key schema")]
    private partial void LogMigrated();
}
