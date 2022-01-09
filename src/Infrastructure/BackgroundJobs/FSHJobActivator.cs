using DN.WebApi.Application.Identity.Users;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Infrastructure.Common;
using DN.WebApi.Shared.Multitenancy;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.BackgroundJobs;

public class FSHJobActivator : JobActivator
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FSHJobActivator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public override JobActivatorScope BeginScope(PerformContext context)
    {
        return new Scope(context, _scopeFactory.CreateScope());
    }

    private class Scope : JobActivatorScope, IServiceProvider
    {
        private readonly PerformContext _context;
        private readonly IServiceScope _scope;

        public Scope(PerformContext context, IServiceScope scope)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));

            SetParameters();
        }

        private void SetParameters()
        {
            string tenant = _context.GetJobParameter<string>(MultitenancyConstants.TenantHeaderKey);
            if (!string.IsNullOrEmpty(tenant))
            {
                ITenantService tenantService = _scope.ServiceProvider.GetRequiredService<ITenantService>();
                tenantService.SetCurrentTenant(tenant);
            }

            string userId = _context.GetJobParameter<string>(QueryStringKeys.UserId);
            if (!string.IsNullOrEmpty(userId))
            {
                ICurrentUser currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();
                currentUser.SetUserJob(userId);
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