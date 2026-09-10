using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Persistence.DataProtection;

/// <summary>
/// Persists ASP.NET Core Data Protection keys to the application database.
/// </summary>
/// <remarks>
/// <para>
/// The alternative store is Redis, which stays the default. Database-backed keys matter when the
/// hosts that protect and unprotect data do not reliably share a Redis instance: the DbMigrator is
/// normally run standalone, outside the AppHost wiring that injects a Redis connection string, so
/// anything its seed encrypts can become permanently undecryptable by the API — a
/// <c>CryptographicException: key {guid} not found in the key ring</c> that no amount of pinning
/// the application name will fix, because the two hosts simply have different key rings. Redis
/// eviction has the same effect on a live system: keys stored in a cache can be evicted, taking
/// every session and pending reset token with them.
/// </para>
/// <para>
/// Deliberately a plain <see cref="DbContext"/> rather than <c>BaseDbContext</c>: Data Protection
/// keys are global framework infrastructure, not tenant data, so they are neither tenant-filtered
/// nor duplicated into a tenant's dedicated database.
/// </para>
/// <para>
/// Provider-neutral. It is wired through <c>AddHeroDbContext</c> like every other context, so it
/// follows <c>DatabaseOptions:Provider</c>, and each provider's migrations project carries its own
/// <c>DataProtection/</c> folder for it.
/// </para>
/// </remarks>
public sealed class DataProtectionKeysDbContext(DbContextOptions<DataProtectionKeysDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
