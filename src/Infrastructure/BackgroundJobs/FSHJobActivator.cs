using Finbuckle.MultiTenant;
using FSH.WebApi.Infrastructure.Auth;
using FSH.WebApi.Infrastructure.Common;
using FSH.WebApi.Infrastructure.Multitenancy;
using FSH.WebApi.Shared.Multitenancy;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.BackgroundJobs;

public class FSHJobActivator : JobActivator
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FSHJobActivator(IServiceScopeFactory scopeFactory) =>
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    public override JobActivatorScope BeginScope(PerformContext context) =>
        new Scope(context, _scopeFactory.CreateScope());

    private class Scope : JobActivatorScope, IServiceProvider
    {
        private readonly PerformContext _context;
        private readonly IServiceScope _scope;

        public Scope(PerformContext context, IServiceScope scope)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));

            ReceiveParameters();
        }

        private void ReceiveParameters()
        {
            string? tenantId = _context.GetJobParameter<string>(MultitenancyConstants.TenantIdName);
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var tenantInfo = _scope.ServiceProvider.GetRequiredService<TenantDbContext>()
                    .TenantInfo.FirstOrDefault(t => t.Identifier == tenantId);
                if (tenantInfo is not null)
                {
                    _scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor>()
                        .MultiTenantContext =
                            new MultiTenantContext<FSHTenantInfo> { TenantInfo = tenantInfo };
                }
            }

            string userId = _context.GetJobParameter<string>(QueryStringKeys.UserId);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                _scope.ServiceProvider.GetRequiredService<ICurrentUserInitializer>()
                    .SetCurrentUserId(userId);
            }
        }

        public override object Resolve(Type type) =>
            ActivatorUtilities.GetServiceOrCreateInstance(this, type);

        object? IServiceProvider.GetService(Type serviceType) =>
            serviceType == typeof(PerformContext)
                ? _context
                : _scope.ServiceProvider.GetService(serviceType);
    }
}