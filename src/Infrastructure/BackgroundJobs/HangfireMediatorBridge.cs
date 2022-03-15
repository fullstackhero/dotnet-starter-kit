using System.ComponentModel;
using Finbuckle.MultiTenant;
using FSH.WebApi.Infrastructure.Auth;
using FSH.WebApi.Infrastructure.Multitenancy;
using MediatR;

namespace FSH.WebApi.Infrastructure.BackgroundJobs;

internal class HangfireMediatorBridge
{
    private readonly IMediator _mediator;
    private readonly IMultiTenantStore<FSHTenantInfo> _tenantStore;
    private readonly IMultiTenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserInitializer _currentUserInitializer;

    public HangfireMediatorBridge(IMediator mediator, IMultiTenantStore<FSHTenantInfo> tenantStore, IMultiTenantContextAccessor tenantContextAccessor, ICurrentUserInitializer currentUserInitializer)
    {
        _mediator = mediator;
        _tenantStore = tenantStore;
        _tenantContextAccessor = tenantContextAccessor;
        _currentUserInitializer = currentUserInitializer;
    }

    // This method can be used for "Enqueue"-ing or "Schedule"-ing jobs. In this case the
    // current tenant and current user are handled by FSHJobFilter and FSHJobActivator
    [DisplayName("{0}")]
    public Task Send(string jobName, IRequest request, CancellationToken ct) => _mediator.Send(request, ct);

    // This method has to be used for recurring jobs (with "AddOrUpdate") because in that case
    // FSHJobFilter.OnCreating is called outside of the context of a request (there's no HttpContext available)
    // so you have to supply tenantId and userId which are then resolved again here (inside the hangfire job)
    // before actually sending the request.
    // See also https://github.com/HangfireIO/Hangfire/pull/570 and https://github.com/HangfireIO/Hangfire/pull/1420
    // which would add a "parameters" argument to AddOrUpdate so FSHJobActivator could also be used for this in stead of this "hack".
    [DisplayName("{0}")]
    public async Task Send(string jobName, string tenantId, string userId, IRequest request, CancellationToken ct)
    {
        _tenantContextAccessor.MultiTenantContext =
            await _tenantStore.TryGetAsync(tenantId) is { } tenantInfo
                ? new MultiTenantContext<FSHTenantInfo>() { TenantInfo = tenantInfo }
                : throw new InvalidOperationException("Invalid tenant.");

        _currentUserInitializer.SetCurrentUserId(userId);

        await _mediator.Send(request, ct);
    }
}