using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;

namespace FSH.Modules.Multitenancy.Services;

/// <summary>
/// Finbuckle-backed <see cref="IEventTenantScope"/>. Installs the ambient tenant context
/// (an AsyncLocal in Finbuckle) for the duration of an integration-event dispatch so that
/// handler DbContexts resolved afterwards capture a real <c>TenantInfo</c> instead of the
/// null one a background scope would otherwise carry.
///
/// Mirrors the create-scope-then-set-tenant pattern already used by
/// <c>WebhookDispatchJob</c> / <c>SqlAuditSink</c>, generalized to the event pipeline.
/// Only tenant identity is set (sufficient for the row-level tenant query filter in the
/// shared-database model); per-tenant connection strings are not resolved here.
/// </summary>
public sealed class FinbuckleEventTenantScope : IEventTenantScope
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;
    private readonly IMultiTenantContextSetter _setter;

    public FinbuckleEventTenantScope(
        IMultiTenantContextAccessor<AppTenantInfo> accessor,
        IMultiTenantContextSetter setter)
    {
        _accessor = accessor;
        _setter = setter;
    }

    public IDisposable Begin(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            // Global events: leave the ambient context untouched.
            return NoopScope.Instance;
        }

        var previous = _accessor.MultiTenantContext;
        _setter.MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(tenantId, tenantId));

        return new RestoreScope(_setter, previous);
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly IMultiTenantContextSetter _setter;
        private readonly IMultiTenantContext<AppTenantInfo> _previous;

        public RestoreScope(IMultiTenantContextSetter setter, IMultiTenantContext<AppTenantInfo> previous)
        {
            _setter = setter;
            _previous = previous;
        }

        public void Dispose() => _setter.MultiTenantContext = _previous;
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}
