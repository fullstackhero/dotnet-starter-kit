using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Provisioning;
using FSH.Modules.Multitenancy.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Multitenancy.Tests.Provisioning;

public class TenantProvisioningJobTests
{
    [Fact]
    public async Task RunAsync_ShouldPassCancellationToken_ToProvisioningService()
    {
        // Arrange
        var provisioningService = Substitute.For<ITenantProvisioningService>();
        var tenantStore = Substitute.For<IMultiTenantStore<AppTenantInfo>>();
        var tenantContextSetter = Substitute.For<IMultiTenantContextSetter>();
        var tenantService = Substitute.For<ITenantService>();
        var logger = Substitute.For<ILogger<TenantProvisioningJob>>();

        var job = new TenantProvisioningJob(
            provisioningService,
            tenantStore,
            tenantContextSetter,
            tenantService,
            logger);

        var tenantId = "test-tenant";
        var correlationId = "corr-123";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to ensure it throws or passes the cancelled token

        tenantStore.GetAsync(tenantId).Returns(new AppTenantInfo(tenantId, "Test Tenant", null, "admin@test.com", null));
        
        // Mock MarkRunningAsync to throw if the cancellation token is indeed cancelled
        provisioningService.MarkRunningAsync(tenantId, correlationId, Arg.Any<TenantProvisioningStepName>(), Arg.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .Returns(Task.FromException<bool>(new OperationCanceledException()));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => job.RunAsync(tenantId, correlationId, cts.Token));
    }
}
