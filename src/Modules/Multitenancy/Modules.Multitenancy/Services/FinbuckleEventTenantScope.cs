using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Finbuckle.MultiTenant.Stores;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Multitenancy.Services;

/// <summary>
/// Finbuckle-backed <see cref="IEventTenantScope"/>. Installs the ambient tenant context
/// (an AsyncLocal in Finbuckle) for the duration of an integration-event dispatch so that
/// handler DbContexts resolved afterwards capture a real <c>TenantInfo</c> instead of the
/// null one a background scope would otherwise carry.
///
/// <see cref="ExecuteAsync"/> resolves the full tenant (including a dedicated connection string)
/// before the action constructs consumers, in the same async flow.
/// </summary>
public sealed class FinbuckleEventTenantScope : IEventTenantScope
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;
    private readonly IMultiTenantContextSetter _setter;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FinbuckleEventTenantScope> _logger;

    public FinbuckleEventTenantScope(
        IMultiTenantContextAccessor<AppTenantInfo> accessor,
        IMultiTenantContextSetter setter,
        IServiceScopeFactory scopeFactory,
        ILogger<FinbuckleEventTenantScope> logger)
    {
        _accessor = accessor;
        _setter = setter;
        _scopeFactory = scopeFactory;
        _logger = logger;
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

    public async Task ExecuteAsync(
        string? tenantId,
        Func<CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        var previous = _accessor.MultiTenantContext;
        if (string.IsNullOrWhiteSpace(tenantId) ||
            (string.Equals(previous.TenantInfo?.Id, tenantId, StringComparison.Ordinal) &&
             !string.IsNullOrWhiteSpace(previous.TenantInfo?.ConnectionString)))
        {
            await action(ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var tenant = await FindTenantAsync(tenantId, ct).ConfigureAwait(false);
            if (tenant is null || !tenant.IsActive)
            {
                _logger.LogWarning("Event tenant {TenantId} is missing or inactive; dispatching with tenant identity only", tenantId);
                tenant = new AppTenantInfo(tenantId, tenantId);
            }

            _setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
            await action(ct).ConfigureAwait(false);
        }
        finally
        {
            _setter.MultiTenantContext = previous;
        }
    }

    private async Task<AppTenantInfo?> FindTenantAsync(string tenantId, CancellationToken ct)
    {
        // A single IMultiTenantStore resolve returns only the last registration (the EF store), so enumerate all of them.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var stores = scope.ServiceProvider.GetServices<IMultiTenantStore<AppTenantInfo>>().ToList();
        var cache = stores.OfType<DistributedCacheStore<AppTenantInfo>>().Single();

        var tenant = await cache.GetAsync(tenantId).WaitAsync(ct).ConfigureAwait(false);
        if (tenant is not null)
        {
            return tenant;
        }

        var efStore = stores.OfType<EFCoreStore<TenantDbContext, AppTenantInfo>>().Single();
        tenant = await efStore.GetAsync(tenantId).WaitAsync(ct).ConfigureAwait(false);
        if (tenant is not null)
        {
            await cache.AddAsync(tenant).ConfigureAwait(false);
        }

        return tenant;
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
