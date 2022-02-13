using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Catalog.Products.RecurringJobs;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Infrastructure.Multitenancy;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.BackgroundJobs.RecurringJobs;

public class RecurringJobInitialization : IRecurringJobInitialization
{
    private readonly ILogger<RecurringJobInitialization> _logger;
    private readonly IJobService _jobService;
    private readonly TenantDbContext _tenantDbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantInfo _tenantInfo;

    public RecurringJobInitialization(ILogger<RecurringJobInitialization> logger, IJobService jobService, TenantDbContext tenantDbContext, IServiceProvider serviceProvider, ITenantInfo tenantInfo)
    {
        _logger = logger;
        _jobService = jobService;
        _tenantDbContext = tenantDbContext;
        _serviceProvider = serviceProvider;
        _tenantInfo = tenantInfo;
    }

    public async Task InitializeJobsForTenantAsync(CancellationToken cancellationToken)
    {
        foreach (var tenant in await _tenantDbContext.TenantInfo.ToListAsync(cancellationToken))
        {
            InitializeJobsForTenant(tenant);
        }
    }

    public void InitializeJobsForTenant(FSHTenantInfo tenant)
    {
        // First create a new scope
        using var scope = _serviceProvider.CreateScope();

        // Then set current tenant so the right connectionstring is used
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor>()
            .MultiTenantContext = new MultiTenantContext<FSHTenantInfo>()
            {
                TenantInfo = tenant
            };

        scope.ServiceProvider.GetRequiredService<IRecurringJobInitialization>()
            .InitializeRecurringJobs();
    }

    public void InitializeRecurringJobs()
    {
        _jobService.AddOrUpdate<ICheckProductOrdersJob>("Id", x => x.CheckInProductOrders(), () => Cron.Minutely(), TimeZoneInfo.Utc, "default");
        _logger.LogInformation("All recurring jobs have been initialized.");
    }
}
