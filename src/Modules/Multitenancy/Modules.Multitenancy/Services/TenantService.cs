using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Stores;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenants;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Features.v1.GetTenants;
using FSH.Modules.Multitenancy.Provisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Multitenancy.Services;

public sealed class TenantService : ITenantService
{
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly DatabaseOptions _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantProvisioningService _provisioningService;
    private readonly TimeProvider _timeProvider;
    private readonly TenantBillingOptions _billingOptions;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IOptions<DatabaseOptions> config,
        IServiceProvider serviceProvider,
        TenantDbContext dbContext,
        ITenantProvisioningService provisioningService,
        TimeProvider timeProvider,
        IOptions<TenantBillingOptions> billingOptions,
        ILogger<TenantService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(billingOptions);
        _tenantStore = tenantStore;
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _provisioningService = provisioningService;
        _timeProvider = timeProvider;
        _billingOptions = billingOptions.Value;
        _logger = logger;
    }

    public async Task<string> ActivateAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);

        if (tenant.IsActive)
        {
            throw new CustomException($"tenant {id} is already activated");
        }

        await _provisioningService.EnsureCanActivateAsync(id, cancellationToken).ConfigureAwait(false);

        tenant.Activate();

        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);

        return $"tenant {id} is now activated";
    }

    public async Task<string> CreateAsync(string id,
        string name,
        string? connectionString,
        string adminEmail, string? issuer, string planKey, DateTime validUpto, CancellationToken cancellationToken)
    {
        if (connectionString?.Trim() == _config.ConnectionString.Trim())
        {
            connectionString = string.Empty;
        }

        AppTenantInfo tenant = new(id, name, connectionString, adminEmail, issuer)
        {
            Plan = planKey,
            // Set ValidUpto directly to the plan term: SetValidity() forbids moving the date backward, and
            // the ctor seeds now+1mo, so it would reject a term computed from an earlier 'now'.
            ValidUpto = DateTime.SpecifyKind(validUpto, DateTimeKind.Utc),
        };
        await _tenantStore.AddAsync(tenant).ConfigureAwait(false);
        await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);

        return tenant.Id;
    }

    public async Task MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SeedTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> DeactivateAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);
        if (!tenant.IsActive)
        {
            throw new CustomException($"tenant {id} is already deactivated");
        }

        int tenantCount = (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Count(t => t.IsActive);
        if (tenantCount <= 1)
        {
            throw new CustomException("At least one active tenant is required.");
        }

        if (tenant.Id.Equals(MultitenancyConstants.Root.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new CustomException("The root tenant cannot be deactivated.");
        }

        tenant.Deactivate();
        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);
        return $"tenant {id} is now deactivated";
    }

    public async Task<bool> ExistsWithIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _tenantStore.GetAsync(id).ConfigureAwait(false) is not null;

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default) =>
        (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Any(t => t.Name == name);

    public async Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<AppTenantInfo> tenants = _dbContext.TenantInfo;
        var specification = new GetTenantsSpecification(query);
        IQueryable<TenantDto> projected = tenants.ApplySpecification(specification);

        return await projected
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantStatusDto> GetStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);

        var graceEnds = tenant.ValidUpto.AddDays(_billingOptions.GracePeriodDays);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        string expiryState;
        if (now <= tenant.ValidUpto)
        {
            expiryState = "Active";
        }
        else if (now <= graceEnds)
        {
            expiryState = "InGrace";
        }
        else
        {
            expiryState = "Expired";
        }

        return new TenantStatusDto
        {
            Id = tenant.Id!,
            Name = tenant.Name!,
            IsActive = tenant.IsActive,
            ValidUpto = tenant.ValidUpto,
            HasConnectionString = !string.IsNullOrWhiteSpace(tenant.ConnectionString),
            AdminEmail = tenant.AdminEmail!,
            Issuer = tenant.Issuer,
            Plan = tenant.Plan,
            ExpiryState = expiryState,
            GraceEndsUtc = graceEnds
        };
    }

    public async Task<(DateTime PeriodStartUtc, DateTime ValidUpto, bool PlanChanged)> RenewAsync(
        string id, string newPlanKey, int termMonths, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPlanKey);

        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Stack remaining time: renew from ValidUpto if still in the future, otherwise from now.
        var periodStart = DateTime.SpecifyKind(tenant.ValidUpto > now ? tenant.ValidUpto : now, DateTimeKind.Utc);
        var newValidUpto = DateTime.SpecifyKind(periodStart.AddMonths(termMonths), DateTimeKind.Utc);
        var planChanged = !string.Equals(tenant.Plan, newPlanKey, StringComparison.OrdinalIgnoreCase);

        tenant.SetValidity(newValidUpto);
        if (planChanged)
        {
            tenant.Plan = newPlanKey;
        }

        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);

        return (periodStart, newValidUpto, planChanged);
    }

    public async Task<DateTime> AdjustValidityAsync(string id, DateTime validUpto, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);

        // Set directly rather than via SetValidity: this operator override is allowed to move the date
        // backward (e.g. immediate expiry / correcting a mistake), which SetValidity forbids.
        var normalized = DateTime.SpecifyKind(validUpto, DateTimeKind.Utc);
        var previous = tenant.ValidUpto;
        tenant.ValidUpto = normalized;

        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[Multitenancy] operator adjusted tenant {TenantId} validity from {Previous:o} to {ValidUpto:o}",
                id, previous, normalized);
        }

        return normalized;
    }

    private async Task<AppTenantInfo> GetTenantInfoAsync(string id, CancellationToken cancellationToken = default) =>
        await _tenantStore.GetAsync(id).ConfigureAwait(false)
            ?? throw new NotFoundException($"{typeof(AppTenantInfo).Name} {id} Not Found.");

    // Finbuckle resolves via the distributed-cache store first (60-min TTL) while the injected store only
    // writes EF, so push the new state into the cache store too — otherwise flips lag until cache expiry.
    private async Task RefreshTenantCacheAsync(AppTenantInfo tenant)
    {
        var cacheStore = _serviceProvider
            .GetServices<IMultiTenantStore<AppTenantInfo>>()
            .FirstOrDefault(s => s.GetType() == typeof(DistributedCacheStore<AppTenantInfo>));
        if (cacheStore is not null)
        {
            await cacheStore.UpdateAsync(tenant).ConfigureAwait(false);
        }
    }
}