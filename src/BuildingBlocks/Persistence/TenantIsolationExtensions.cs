using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Persistence;

/// <summary>
/// Tenant-isolation defaults for <see cref="ModelBuilder"/>. Applied from
/// <see cref="Context.BaseDbContext"/> and <c>IdentityDbContext</c> so every
/// entity in the model is tenant-scoped unless it explicitly opts out with
/// the <see cref="IGlobalEntity"/> marker interface. This makes the DEFAULT
/// behaviour tenant-isolated — adding a new entity to a module can no
/// longer silently leak data across tenants.
/// </summary>
public static class TenantIsolationExtensions
{
    /// <summary>Finbuckle's per-entity annotation key. Reading this lets us skip
    /// entities that already opted in via explicit <c>builder.IsMultiTenant()</c>.</summary>
    private const string FinbuckleMultiTenantAnnotation = "Finbuckle:MultiTenant";

    /// <summary>
    /// Iterates every non-owned entity in <paramref name="modelBuilder"/> and
    /// marks it <c>IsMultiTenant().AdjustUniqueIndexes()</c> unless it is an
    /// <see cref="IGlobalEntity"/> or already explicitly marked.
    ///
    /// Call AFTER <c>ApplyConfigurationsFromAssembly</c> so per-entity configs
    /// (unique indexes, owned types) are already applied — Finbuckle's
    /// AdjustUniqueIndexes needs those to know what to widen.
    /// </summary>
    public static void ApplyTenantIsolationByDefault(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (entityType.ClrType is null) continue;
            // Skip keyless value-typed sub-models (e.g. IdentityPasskeyData) that ride in the EF
            // model but aren't persisted entities; IsMultiTenant() on them throws "missing primary key".
            if (entityType.FindPrimaryKey() is null) continue;
            if (typeof(IGlobalEntity).IsAssignableFrom(entityType.ClrType)) continue;
            if (entityType.FindAnnotation(FinbuckleMultiTenantAnnotation) is not null) continue;

            modelBuilder.Entity(entityType.ClrType).IsMultiTenant().AdjustUniqueIndexes();
        }
    }
}
