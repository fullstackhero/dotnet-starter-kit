using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Inbox;
using FSH.Framework.Eventing.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Framework.Tests.Eventing;

/// <summary>
/// #1423: one failing handler for an event must not stop the other handlers of that event. Every handler
/// runs, the failure still surfaces (so the outbox retries), and the inbox makes the retry re-run only the
/// handler that failed.
/// </summary>
public sealed class InMemoryEventBusHandlerIsolationTests
{
    [Fact]
    public async Task PublishAsync_Should_RunEveryHandler_And_StillThrow_When_AnEarlierHandlerFails()
    {
        // Arrange — the failing handler is registered first, as Files' delete runs before Catalog's rewrite.
        var failing = new FailingHandler(failuresLeft: 1);
        var succeeding = new CountingHandler();
        var inbox = new RecordingInbox();
        using var provider = BuildProvider(inbox, failing, succeeding);
        var bus = new InMemoryEventBus(provider, NullLogger<InMemoryEventBus>.Instance, new NullScope());
        var @event = new TestEvent();

        // Act
        var act = () => bus.PublishAsync(@event);

        // Assert — the later handler ran even though the first one threw, and the failure still surfaced.
        (await act.ShouldThrowAsync<InvalidOperationException>()).Message.ShouldBe(FailingHandler.Message);
        succeeding.Calls.ShouldBe(1);
        inbox.Processed.ShouldContain((@event.Id, typeof(CountingHandler).FullName!));
        inbox.Processed.ShouldNotContain((@event.Id, typeof(FailingHandler).FullName!));
    }

    [Fact]
    public async Task PublishAsync_Should_ReRunOnlyTheFailedHandler_When_TheEventIsRetried()
    {
        // Arrange
        var failing = new FailingHandler(failuresLeft: 1);
        var succeeding = new CountingHandler();
        using var provider = BuildProvider(new RecordingInbox(), failing, succeeding);
        var bus = new InMemoryEventBus(provider, NullLogger<InMemoryEventBus>.Instance, new NullScope());
        var @event = new TestEvent();
        await Should.ThrowAsync<InvalidOperationException>(() => bus.PublishAsync(@event));

        // Act — the outbox's retry publishes the same event again.
        await bus.PublishAsync(@event);

        // Assert — the handler that already succeeded is skipped by the inbox; the failed one ran again.
        succeeding.Calls.ShouldBe(1);
        failing.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_Should_ThrowAggregateException_When_SeveralHandlersFail()
    {
        // Arrange
        using var provider = BuildProvider(
            new RecordingInbox(), new FailingHandler(failuresLeft: 1), new SecondFailingHandler(), new CountingHandler());
        var bus = new InMemoryEventBus(provider, NullLogger<InMemoryEventBus>.Instance, new NullScope());

        // Act
        var act = () => bus.PublishAsync(new TestEvent());

        // Assert
        (await act.ShouldThrowAsync<AggregateException>()).InnerExceptions.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_Should_GiveEachHandlerAFreshScope_So_AFailedHandlersPendingChangesAreNotSavedByTheNext()
    {
        // Arrange - both handlers share a scoped "unit of work", like two handlers on one module DbContext.
        // The first stages a change and throws before saving; the second saves whatever is staged.
        var saved = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IInboxStore>(new RecordingInbox());
        services.AddSingleton(saved);
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IIntegrationEventHandler<TestEvent>, StagesThenThrowsHandler>();
        services.AddScoped<IIntegrationEventHandler<TestEvent>, SavesStagedHandler>();
        using var provider = services.BuildServiceProvider();
        var bus = new InMemoryEventBus(provider, NullLogger<InMemoryEventBus>.Instance, new NullScope());

        // Act
        await Should.ThrowAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent()));

        // Assert - the second handler ran in its own scope, so the failed handler's staged change was dropped.
        saved.ShouldBe(["saved-by-second"]);
    }

    private static ServiceProvider BuildProvider(IInboxStore inbox, params IIntegrationEventHandler<TestEvent>[] handlers)
    {
        var services = new ServiceCollection();
        services.AddSingleton(inbox);
        foreach (var handler in handlers)
        {
            services.AddSingleton(handler);
        }

        return services.BuildServiceProvider();
    }

    #region Test doubles

    private sealed record TestEvent : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.CreateVersion7();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string? TenantId { get; } = "acme";
        public string CorrelationId { get; } = Guid.CreateVersion7().ToString();
        public string Source { get; } = "tests";
    }

    private sealed class FailingHandler(int failuresLeft) : IIntegrationEventHandler<TestEvent>
    {
        public const string Message = "store rejected the delete";
        private int _failuresLeft = failuresLeft;

        public int Calls { get; private set; }

        public async Task HandleAsync(TestEvent @event, CancellationToken ct = default)
        {
            await Task.Yield();
            Calls++;
            if (_failuresLeft-- > 0)
            {
                throw new InvalidOperationException(Message);
            }
        }
    }

    private sealed class SecondFailingHandler : IIntegrationEventHandler<TestEvent>
    {
        public async Task HandleAsync(TestEvent @event, CancellationToken ct = default)
        {
            await Task.Yield();
            throw new InvalidOperationException("second handler failed");
        }
    }

    private sealed class CountingHandler : IIntegrationEventHandler<TestEvent>
    {
        public int Calls { get; private set; }

        public Task HandleAsync(TestEvent @event, CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWork
    {
        public List<string> Staged { get; } = [];
    }

    private sealed class StagesThenThrowsHandler(UnitOfWork work) : IIntegrationEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken ct = default)
        {
            work.Staged.Add("partial-from-failed-handler");
            throw new InvalidOperationException("failed before saving");
        }
    }

    private sealed class SavesStagedHandler(UnitOfWork work, List<string> saved) : IIntegrationEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken ct = default)
        {
            work.Staged.Add("saved-by-second");
            saved.AddRange(work.Staged);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingInbox : IInboxStore
    {
        public HashSet<(Guid EventId, string Handler)> Processed { get; } = [];

        public Task<bool> HasProcessedAsync(Guid eventId, string handlerName, CancellationToken ct = default)
            => Task.FromResult(Processed.Contains((eventId, handlerName)));

        public Task MarkProcessedAsync(Guid eventId, string handlerName, string? tenantId, string eventType, CancellationToken ct = default)
        {
            Processed.Add((eventId, handlerName));
            return Task.CompletedTask;
        }
    }

    private sealed class NullScope : IEventTenantScope
    {
        public IDisposable Begin(string? tenantId) => new Noop();

        private sealed class Noop : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    #endregion
}
