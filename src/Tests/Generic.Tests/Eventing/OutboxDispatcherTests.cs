using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using System.Text.Json;

namespace Generic.Tests.Eventing;

public class OutboxDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ShouldRestoreTenantContext()
    {
        // Arrange
        var bus = Substitute.For<IEventBus>();
        var serializer = Substitute.For<IEventSerializer>();
        var outboxStore = Substitute.For<IOutboxStore>();
        var tenantStore = Substitute.For<IMultiTenantStore<AppTenantInfo>>();
        var contextSetter = Substitute.For<IMultiTenantContextSetter>();
        var logger = Substitute.For<ILogger<OutboxDispatcher>>();
        var options = Substitute.For<IOptions<FSH.Framework.Eventing.EventingOptions>>();
        options.Value.Returns(new FSH.Framework.Eventing.EventingOptions { OutboxBatchSize = 100, OutboxMaxRetries = 5 });

        var dispatcher = new OutboxDispatcher(outboxStore, bus, serializer, options, logger, tenantStore, contextSetter);

        var tenantId = "test-tenant-456";
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "DummyEventAssemblyQualifiedName",
            Payload = "{ }",
            TenantId = tenantId
        };

        var tenantInfo = new AppTenantInfo(tenantId, "Test Tenant", null, "admin", null);
        tenantStore.GetAsync(tenantId).Returns(tenantInfo);
        
        outboxStore.GetPendingBatchAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { outboxMessage });

        var dummyEvent = Substitute.For<IIntegrationEvent>();
        serializer.Deserialize(outboxMessage.Payload, outboxMessage.Type).Returns(dummyEvent);

        // Act
        await dispatcher.DispatchAsync(CancellationToken.None);

        // Assert
        // EVENTING-1: Verify context was restored
        await tenantStore.Received(1).GetAsync(tenantId);
        
        contextSetter.Received(1).MultiTenantContext = 
            Arg.Is<IMultiTenantContext<AppTenantInfo>>(ctx => ctx.TenantInfo!.Id == tenantId);
            
        await bus.Received(1).PublishAsync(dummyEvent, Arg.Any<CancellationToken>());
    }
}
