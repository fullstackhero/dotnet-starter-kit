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

    [DisplayName("{0}")]
    public Task Send(string jobName, IRequest request, CancellationToken ct) => _mediator.Send(request, ct);
}