using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Multitenancy.Provisioning;

/// <summary>
/// Initializes the tenant catalog database and seeds the root tenant on startup.
/// </summary>
public sealed class TenantStoreInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantStoreInitializerHostedService> _logger;

    public TenantStoreInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<TenantStoreInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        await tenantDbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Applied tenant catalog migrations.");

        if (await tenantDbContext.TenantInfo.FindAsync([MultitenancyConstants.Root.Id], cancellationToken).ConfigureAwait(false) is null)
        {
            var rootTenant = new AppTenantInfo(
                MultitenancyConstants.Root.Id,
                MultitenancyConstants.Root.Name,
                string.Empty,
                MultitenancyConstants.Root.EmailAddress,
                issuer: MultitenancyConstants.Root.Issuer);

            var validUpto = DateTime.UtcNow.AddYears(1);
            rootTenant.SetValidity(validUpto);
            await tenantDbContext.TenantInfo.AddAsync(rootTenant, cancellationToken).ConfigureAwait(false);
            await tenantDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Seeded root tenant.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
