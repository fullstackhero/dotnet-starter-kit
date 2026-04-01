using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Multitenancy.Provisioning;

public sealed class TenantProvisioningJob(
    ITenantProvisioningService provisioningService,
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IMultiTenantContextSetter tenantContextSetter,
    ITenantService tenantService,
    ILogger<TenantProvisioningJob> logger)
{
    public async Task RunAsync(string tenantId, string correlationId, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantStore.GetAsync(tenantId).ConfigureAwait(false)
            ?? throw new NotFoundException($"Tenant {tenantId} not found during provisioning.");

        var currentStep = TenantProvisioningStepName.Database;
        try
        {
            var runDatabase = await provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);

            tenantContextSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            if (runDatabase)
            {
                await provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);
            }

            currentStep = TenantProvisioningStepName.Migrations;
            var runMigrations = await provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);
            if (runMigrations)
            {
                await tenantService.MigrateTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
                await provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);
            }

            currentStep = TenantProvisioningStepName.Seeding;
            var runSeeding = await provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);
            if (runSeeding)
            {
                await tenantService.SeedTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
                await provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);
            }

            currentStep = TenantProvisioningStepName.CacheWarm;
            var runCacheWarm = await provisioningService.MarkRunningAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);
            if (runCacheWarm)
            {
                await provisioningService.MarkStepCompletedAsync(tenantId, correlationId, currentStep, cancellationToken).ConfigureAwait(false);
            }

            await provisioningService.MarkCompletedAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Provisioned tenant {TenantId} correlation {CorrelationId}", tenantId, correlationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Provisioning failed for tenant {TenantId}", tenantId);
            await provisioningService.MarkFailedAsync(tenantId, correlationId, currentStep, ex.Message, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}