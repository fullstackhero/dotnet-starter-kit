using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Multitenancy.Provisioning;

public sealed class TenantProvisioningJob
{
    private readonly ITenantProvisioningService _provisioningService;
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly IMultiTenantContextSetter _tenantContextSetter;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantProvisioningJob> _logger;

    public TenantProvisioningJob(
        ITenantProvisioningService provisioningService,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IMultiTenantContextSetter tenantContextSetter,
        ITenantService tenantService,
        ILogger<TenantProvisioningJob> logger)
    {
        _provisioningService = provisioningService;
        _tenantStore = tenantStore;
        _tenantContextSetter = tenantContextSetter;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task RunAsync(string tenantId, string correlationId)
    {
        var tenant = await _tenantStore.GetAsync(tenantId).ConfigureAwait(false)
            ?? throw new NotFoundException($"Tenant {tenantId} not found during provisioning.");

        var currentStep = TenantProvisioningStepName.Database;
        try
        {
            var runDatabase = await _provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);

            _tenantContextSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            if (runDatabase)
            {
                await _provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);
            }

            currentStep = TenantProvisioningStepName.Migrations;
            var runMigrations = await _provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);
            if (runMigrations)
            {
                await _tenantService.MigrateTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
                await _provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);
            }

            currentStep = TenantProvisioningStepName.Seeding;
            var runSeeding = await _provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);
            if (runSeeding)
            {
                await _tenantService.SeedTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
                await _provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);
            }

            currentStep = TenantProvisioningStepName.CacheWarm;
            var runCacheWarm = await _provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);
            if (runCacheWarm)
            {
                await _provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, CancellationToken.None).ConfigureAwait(false);
            }

            await _provisioningService.MarkCompletedAsync(tenantId, correlationId, CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation("Provisioned tenant {TenantId} correlation {CorrelationId}", tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provisioning failed for tenant {TenantId}", tenantId);
            await _provisioningService.MarkFailedAsync(tenantId, correlationId, currentStep, ex.Message, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
