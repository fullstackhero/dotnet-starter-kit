using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Framework.Tests.Eventing;

/// <summary>
/// Guards the systemic fix for the background-dispatch tenant-context bug: the bus must
/// establish the tenant scope from the event's <see cref="IIntegrationEvent.TenantId"/>
/// BEFORE it resolves handlers (which materialize tenant-filtered DbContexts). Without
/// this, handlers dispatched from the outbox NRE in their tenant query filter.
/// </summary>
public sealed class InMemoryEventBusTenantScopeTests
{
    [Fact]
    public async Task PublishAsync_Should_BeginTenantScope_WithEventTenantId_WhileHandlerRuns()
    {
        // Arrange
        var scope = new RecordingTenantScope();
        var handler = new TenantProbingHandler(scope);

        var services = new ServiceCollection();
        services.AddSingleton<IEventTenantScope>(scope);
        services.AddSingleton<IIntegrationEventHandler<TenantScopedEvent>>(handler);
        using var provider = services.BuildServiceProvider();

        var bus = new InMemoryEventBus(provider, NullLogger<InMemoryEventBus>.Instance, scope);

        // Act
        await bus.PublishAsync(new TenantScopedEvent("acme"));

        // Assert — scope begun with the event's tenant, and it was still active when the
        // handler executed (i.e. before resolution, restored after).
        scope.BegunWith.ShouldHaveSingleItem().ShouldBe("acme");
        handler.ScopeWasActiveDuringHandle.ShouldBeTrue();
        scope.IsActive.ShouldBeFalse("the scope must be disposed once dispatch completes");
    }

    #region Test doubles

    private sealed record TenantScopedEvent(string? TenantId) : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.CreateVersion7();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string CorrelationId { get; } = Guid.CreateVersion7().ToString();
        public string Source { get; } = "tests";
    }

    private sealed class RecordingTenantScope : IEventTenantScope
    {
        public List<string?> BegunWith { get; } = [];
        public bool IsActive { get; private set; }

        public IDisposable Begin(string? tenantId)
        {
            BegunWith.Add(tenantId);
            IsActive = true;
            return new Handle(this);
        }

        private sealed class Handle(RecordingTenantScope owner) : IDisposable
        {
            public void Dispose() => owner.IsActive = false;
        }
    }

    private sealed class TenantProbingHandler(RecordingTenantScope scope)
        : IIntegrationEventHandler<TenantScopedEvent>
    {
        public bool ScopeWasActiveDuringHandle { get; private set; }

        public Task HandleAsync(TenantScopedEvent @event, CancellationToken ct = default)
        {
            ScopeWasActiveDuringHandle = scope.IsActive;
            return Task.CompletedTask;
        }
    }

    #endregion
}
