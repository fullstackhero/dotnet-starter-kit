namespace FSH.Framework.Eventing.Abstractions;

/// <summary>
/// Establishes the ambient tenant context for the duration of an integration-event
/// dispatch, so that handlers — and the tenant-filtered DbContexts they resolve — see
/// the correct tenant even when the event is published from a background scope
/// (outbox dispatcher, hosted services, recurring jobs) that carries no HTTP request
/// and therefore no tenant context.
///
/// The tenant context MUST be set <b>before</b> handlers are resolved: a
/// <c>MultiTenantDbContext</c> captures its <c>TenantInfo</c> at construction time, so
/// setting the tenant after the handler (and its DbContext) has been materialized is
/// too late and leaves the tenant query filter dereferencing a null tenant.
///
/// The default implementation (<c>NullEventTenantScope</c>) is a no-op; the
/// multitenancy composition registers a Finbuckle-backed implementation. Keeping the
/// abstraction here lets the event bus stay tenant-technology-agnostic.
/// </summary>
public interface IEventTenantScope
{
    /// <summary>
    /// Begins a tenant scope for <paramref name="tenantId"/>. Disposing the returned
    /// handle restores the previous ambient tenant. A null/whitespace id leaves the
    /// ambient context unchanged (global, non-tenant-scoped events).
    /// </summary>
    IDisposable Begin(string? tenantId);
}
