using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Multitenancy.Provisioning;

public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly TenantDbContext _dbContext;
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly IJobService _jobService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        TenantDbContext dbContext,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IJobService jobService,
        IServiceScopeFactory scopeFactory,
        ILogger<TenantProvisioningService> logger)
    {
        _dbContext = dbContext;
        _tenantStore = tenantStore;
        _jobService = jobService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<TenantProvisioning> StartAsync(string tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantStore.GetAsync(tenantId).ConfigureAwait(false)
            ?? throw new NotFoundException($"Tenant {tenantId} not found for provisioning.");

        var existing = await GetLatestAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && (existing.Status is TenantProvisioningStatus.Running or TenantProvisioningStatus.Pending))
        {
            throw new CustomException($"Provisioning already running for tenant {tenantId}.");
        }

        var correlationId = Guid.NewGuid().ToString();
        var provisioning = new TenantProvisioning(tenant.Id, correlationId);

        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.Database));
        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.Migrations));
        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.Seeding));
        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.CacheWarm));

        _dbContext.Add(provisioning);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!TryEnsureJobStorage())
        {
            _logger.LogWarning("Background job storage not available; running provisioning inline for tenant {TenantId}.", tenantId);
            provisioning.SetJobId("inline");
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await RunInlineProvisioningAsync(tenant.Id, correlationId, cancellationToken).ConfigureAwait(false);
            return provisioning;
        }

        var jobId = _jobService.Enqueue<TenantProvisioningJob>(job => job.RunAsync(tenant.Id, correlationId));
        provisioning.SetJobId(jobId);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return provisioning;
    }

    public async Task<TenantProvisioning?> GetLatestAsync(string tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<TenantProvisioning>()
            .Include(p => p.Steps)
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantProvisioningStatusDto> GetStatusAsync(string tenantId, CancellationToken cancellationToken)
    {
        var provisioning = await GetLatestAsync(tenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Provisioning not found for tenant {tenantId}.");

        return ToDto(provisioning);
    }

    public async Task EnsureCanActivateAsync(string tenantId, CancellationToken cancellationToken)
    {
        var provisioning = await GetLatestAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (provisioning is null)
        {
            return;
        }

        if (provisioning.Status != TenantProvisioningStatus.Completed)
        {
            throw new CustomException($"Tenant {tenantId} is not provisioned. Status: {provisioning.Status}.");
        }
    }

    public async Task<string> RetryAsync(string tenantId, CancellationToken cancellationToken)
    {
        var provisioning = await StartAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return provisioning.CorrelationId;
    }

    public async Task<bool> MarkRunningAsync(string tenantId, string correlationId, TenantProvisioningStepName step, CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);
        var stepEntity = provisioning.Steps.First(s => s.Step == step);

        if (stepEntity.Status == TenantProvisioningStatus.Completed)
        {
            return false;
        }

        provisioning.MarkRunning(step.ToString());
        stepEntity.MarkRunning();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task MarkStepCompletedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);
        var stepEntity = provisioning.Steps.First(s => s.Step == step);

        if (stepEntity.Status == TenantProvisioningStatus.Completed)
        {
            return;
        }

        stepEntity.MarkCompleted();
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, string error, CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);
        provisioning.MarkFailed(step.ToString(), error);

        var stepEntity = provisioning.Steps.First(s => s.Step == step);
        stepEntity.MarkFailed(error);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkCompletedAsync(string tenantId, string correlationId, CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);

        if (provisioning.Status == TenantProvisioningStatus.Completed)
        {
            return;
        }

        provisioning.MarkCompleted();
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<TenantProvisioning> RequireAsync(string tenantId, string correlationId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<TenantProvisioning>()
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.CorrelationId == correlationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provisioning {correlationId} for tenant {tenantId} not found.");
    }

    private static bool TryEnsureJobStorage()
    {
        try
        {
            _ = JobStorage.Current;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private async Task RunInlineProvisioningAsync(string tenantId, string correlationId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TenantProvisioningJob>();
        await job.RunAsync(tenantId, correlationId).ConfigureAwait(false);
    }

    private static TenantProvisioningStatusDto ToDto(TenantProvisioning provisioning)
    {
        var steps = provisioning.Steps
            .OrderBy(s => s.Step)
            .Select(s => new TenantProvisioningStepDto(
                s.Step.ToString(),
                s.Status.ToString(),
                s.StartedUtc,
                s.CompletedUtc,
                s.Error))
            .ToArray();

        return new TenantProvisioningStatusDto(
            provisioning.TenantId,
            provisioning.Status.ToString(),
            provisioning.CorrelationId,
            provisioning.CurrentStep,
            provisioning.Error,
            provisioning.CreatedUtc,
            provisioning.StartedUtc,
            provisioning.CompletedUtc,
            steps);
    }
}
