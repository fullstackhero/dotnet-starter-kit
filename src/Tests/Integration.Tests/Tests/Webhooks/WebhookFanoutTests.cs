using System.Collections.Concurrent;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Webhooks.Services;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
#pragma warning disable CA1707 // Test method names use underscores by convention

namespace Integration.Tests.Tests.Webhooks;

/// <summary>
/// Exercises the open-generic <see cref="WebhookFanoutHandler{TEvent}"/> bridge: when an
/// <see cref="IIntegrationEvent"/> is published on the <see cref="IEventBus"/>, the handler must look
/// up the publishing tenant's ACTIVE subscriptions, match them against <c>typeof(TEvent).Name</c>, and
/// enqueue one delivery per match via <see cref="IWebhookDispatcher"/>.
///
/// We replace <see cref="IWebhookDispatcher"/> with an in-memory recorder on a derived factory so the
/// enqueue calls are observed synchronously (no Hangfire timing). The recorder is registered in the
/// root DI container — the fanout handler resolves it through a child scope created by the in-memory
/// bus, so a singleton recorder is visible to it. Publishing goes through the real bus, which closes
/// the open generic for our event type.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookFanoutTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public WebhookFanoutTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task Fanout_Should_EnqueueOneDelivery_When_SubscriptionMatchesEventType()
    {
        // Arrange
        var recorder = new RecordingDispatcher();
        await using var capturingFactory = CreateCapturingFactory(recorder);

        var eventType = nameof(FanoutTestEvent);
        var subscriptionId = await CreateSubscriptionAsync(capturingFactory, new[] { eventType });

        // Act — publish a matching event for the root tenant.
        await PublishAsync(capturingFactory, new FanoutTestEvent(TestConstants.RootTenantId));

        // Assert — exactly one enqueue, for our subscription, tagged with the event type name.
        var enqueued = recorder.For(subscriptionId);
        enqueued.Count.ShouldBe(1);
        enqueued[0].TenantId.ShouldBe(TestConstants.RootTenantId);
        enqueued[0].EventType.ShouldBe(eventType);
        enqueued[0].PayloadJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Fanout_Should_EnqueueForWildcardSubscription_When_AnyEventPublished()
    {
        // Arrange — a "*" catch-all subscription.
        var recorder = new RecordingDispatcher();
        await using var capturingFactory = CreateCapturingFactory(recorder);
        var subscriptionId = await CreateSubscriptionAsync(capturingFactory, new[] { "*" });

        // Act
        await PublishAsync(capturingFactory, new FanoutTestEvent(TestConstants.RootTenantId));

        // Assert
        recorder.For(subscriptionId).Count.ShouldBe(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Fanout_Should_NotEnqueue_When_NoSubscriptionMatchesEventType()
    {
        // Arrange — subscription listens for a different event only.
        var recorder = new RecordingDispatcher();
        await using var capturingFactory = CreateCapturingFactory(recorder);
        var subscriptionId = await CreateSubscriptionAsync(capturingFactory, new[] { "some.other.event" });

        // Act
        await PublishAsync(capturingFactory, new FanoutTestEvent(TestConstants.RootTenantId));

        // Assert — no enqueue for this subscription.
        recorder.For(subscriptionId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fanout_Should_Skip_When_EventHasNullTenantId()
    {
        // Arrange — a matching subscription exists, but the event is global (TenantId null).
        var recorder = new RecordingDispatcher();
        await using var capturingFactory = CreateCapturingFactory(recorder);
        var subscriptionId = await CreateSubscriptionAsync(capturingFactory, new[] { nameof(FanoutTestEvent) });

        // Act — global event must be skipped (webhooks are tenant-scoped by design).
        await PublishAsync(capturingFactory, new FanoutTestEvent(TenantId: null));

        // Assert
        recorder.For(subscriptionId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fanout_Should_NotEnqueue_When_MatchingSubscriptionIsInactive()
    {
        // Arrange — create a matching subscription, then deactivate it via DELETE (soft deactivate).
        var recorder = new RecordingDispatcher();
        await using var capturingFactory = CreateCapturingFactory(recorder);
        var subscriptionId = await CreateSubscriptionAsync(capturingFactory, new[] { nameof(FanoutTestEvent) });

        using (var client = await CreateRootClientFor(capturingFactory))
        {
            var del = await client.DeleteAsync(
                $"{TestConstants.WebhooksBasePath}/subscriptions/{subscriptionId}");
            del.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Act
        await PublishAsync(capturingFactory, new FanoutTestEvent(TestConstants.RootTenantId));

        // Assert — the handler filters on IsActive, so a deactivated subscription is skipped.
        recorder.For(subscriptionId).ShouldBeEmpty();
    }

    #endregion

    #region Helpers

    private WebApplicationFactory<Program> CreateCapturingFactory(RecordingDispatcher recorder) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace the real Hangfire-backed dispatcher with an in-memory recorder so enqueue
                // calls are observed synchronously. Singleton so every scope sees the same recorder.
                services.RemoveAll<IWebhookDispatcher>();
                services.AddSingleton<IWebhookDispatcher>(recorder);
            });
        });

    private async Task<HttpClient> CreateRootClientFor(WebApplicationFactory<Program> capturingFactory)
    {
        var token = await _auth.GetRootAdminTokenAsync();
        var client = capturingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        return client;
    }

    private async Task<Guid> CreateSubscriptionAsync(
        WebApplicationFactory<Program> capturingFactory, string[] events)
    {
        using var client = await CreateRootClientFor(capturingFactory);
        var createResponse = await client.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions",
            new
            {
                url = "https://webhook-fanout-test.invalid/receive",
                events,
                secret = "fanout-secret"
            });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await createResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task PublishAsync(WebApplicationFactory<Program> capturingFactory, IIntegrationEvent @event)
    {
        using var scope = capturingFactory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Install the tenant context on the calling scope for safety (the fanout handler installs
        // its own from the event's TenantId, but the in-memory bus creates a child scope).
        if (!string.IsNullOrWhiteSpace(@event.TenantId))
        {
            var tenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
                .GetAsync(@event.TenantId).ConfigureAwait(false);
            if (tenant is not null)
            {
                sp.GetRequiredService<IMultiTenantContextSetter>()
                    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
            }
        }

        var bus = sp.GetRequiredService<IEventBus>();
        await bus.PublishAsync(@event).ConfigureAwait(false);
    }

    /// <summary>
    /// A test integration event. Its runtime type name (<c>FanoutTestEvent</c>) is what
    /// <see cref="WebhookFanoutHandler{TEvent}"/> uses as the event type for subscription matching.
    /// </summary>
    private sealed record FanoutTestEvent(string? TenantId) : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string CorrelationId { get; } = Guid.NewGuid().ToString("N");
        public string Source { get; } = "integration-tests";
    }

    private sealed record EnqueueRecord(string TenantId, Guid SubscriptionId, string EventType, string PayloadJson);

    private sealed class RecordingDispatcher : IWebhookDispatcher
    {
        private readonly ConcurrentBag<EnqueueRecord> _records = [];

        public Task EnqueueAsync(
            string tenantId, Guid subscriptionId, string eventType, string payloadJson,
            CancellationToken cancellationToken = default)
        {
            _records.Add(new EnqueueRecord(tenantId, subscriptionId, eventType, payloadJson));
            return Task.CompletedTask;
        }

        public List<EnqueueRecord> For(Guid subscriptionId) =>
            _records.Where(r => r.SubscriptionId == subscriptionId).ToList();
    }

    #endregion
}
