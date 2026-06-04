namespace FSH.Framework.Core.Domain;

/// <summary>
/// Marker interface — entities that intentionally OPT OUT of automatic
/// tenant isolation. Any entity in a <c>MultiTenantDbContext</c>-derived
/// context that does NOT implement this is auto-marked
/// <c>IsMultiTenant()</c> by <c>BaseDbContext.OnModelCreating</c>.
///
/// Implement this only for rows that are genuinely shared across all
/// tenants — platform billing plans, cross-tenant audit/impersonation
/// records, system catalogs. The default is tenant-isolated.
/// </summary>
public interface IGlobalEntity { }
