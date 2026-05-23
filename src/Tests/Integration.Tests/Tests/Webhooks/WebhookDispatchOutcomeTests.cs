using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Services;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
#pragma warning disable CA1707 // Test method names use underscores by convention

namespace Integration.Tests.Tests.Webhooks;

/// <summary>
/// Covers the <see cref="WebhookDispatchJob"/> outcome branches that the existing
/// WebhookDispatchJobTests do not: a 2xx SUCCESS (no throw, success row persisted) and a
/// non-retryable 4xx PERMANENT failure (no throw — so Hangfire does not reschedule — but a row
/// is still persisted). We steer the outbound HTTP response by overriding the named "Webhooks"
/// HttpClient transport on a derived factory, then invoke the job directly (bypassing Hangfire's
/// scheduler) so the assertions are deterministic.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookDispatchOutcomeTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public WebhookDispatchOutcomeTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task DispatchAsync_Should_PersistSuccessRow_And_NotThrow_When_RemoteReturns200()
    {
        // Arrange
        var transport = new FixedStatusHandler(HttpStatusCode.OK);
        await using var capturingFactory = CreateCapturingFactory(transport);
        var subscriptionId = await CreateSubscriptionAsync(secret: "dispatch-ok-secret");

        // Act — direct invocation; the success path returns without throwing.
        await InvokeDispatchAsync(capturingFactory, subscriptionId, "{\"ok\":true}");

        // Assert
        transport.WasInvoked.ShouldBeTrue();
        var delivery = await ReadLatestDeliveryAsync(capturingFactory, subscriptionId);
        delivery.ShouldNotBeNull();
        delivery!.Success.ShouldBeTrue();
        delivery.HttpStatusCode.ShouldBe(200);
        delivery.AttemptCount.ShouldBe(1);
        delivery.ErrorMessage.ShouldBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DispatchAsync_Should_PersistFailureRow_And_NotThrow_When_RemoteReturnsNonRetryable4xx()
    {
        // Arrange — 400 is a permanent client error: the job records the row and gives up
        // (does NOT throw) so Hangfire won't pointlessly retry a permanent condition.
        var transport = new FixedStatusHandler(HttpStatusCode.BadRequest);
        await using var capturingFactory = CreateCapturingFactory(transport);
        var subscriptionId = await CreateSubscriptionAsync(secret: "dispatch-4xx-secret");

        // Act & Assert — must not throw.
        await Should.NotThrowAsync(() => InvokeDispatchAsync(capturingFactory, subscriptionId, "{\"bad\":true}"));

        var delivery = await ReadLatestDeliveryAsync(capturingFactory, subscriptionId);
        delivery.ShouldNotBeNull();
        delivery!.Success.ShouldBeFalse();
        delivery.HttpStatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task DispatchAsync_Should_Throw_And_PersistFailureRow_When_RemoteReturnsTransient5xx()
    {
        // Arrange — 503 is transient: the job throws so Hangfire reschedules, and still records
        // the attempt. (Existing tests cover the network-error transient path; this covers the
        // HTTP-5xx transient branch.)
        var transport = new FixedStatusHandler(HttpStatusCode.ServiceUnavailable);
        await using var capturingFactory = CreateCapturingFactory(transport);
        var subscriptionId = await CreateSubscriptionAsync(secret: "dispatch-5xx-secret");

        // Act & Assert
        await Should.ThrowAsync<WebhookDeliveryFailedException>(() =>
            InvokeDispatchAsync(capturingFactory, subscriptionId, "{\"retry\":true}"));

        var delivery = await ReadLatestDeliveryAsync(capturingFactory, subscriptionId);
        delivery.ShouldNotBeNull();
        delivery!.Success.ShouldBeFalse();
        delivery.HttpStatusCode.ShouldBe(503);
    }

    [Fact]
    public async Task EnqueueAsync_Should_ScheduleHangfireJob_When_ArgumentsValid()
    {
        // Arrange — the real (Hangfire-backed) dispatcher from the shared factory. Existing tests
        // only cover its argument-guard branches; this covers the happy enqueue path.
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

        // Act & Assert — a valid enqueue against Hangfire InMemory must not throw.
        await Should.NotThrowAsync(() => dispatcher.EnqueueAsync(
            TestConstants.RootTenantId,
            Guid.NewGuid(),
            "user.created",
            "{\"id\":\"1\"}"));
    }

    #endregion

    #region Helpers

    private WebApplicationFactory<Program> CreateCapturingFactory(FixedStatusHandler transport) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient("Webhooks")
                    .ConfigurePrimaryHttpMessageHandler(() => transport);
            });
        });

    private async Task<Guid> CreateSubscriptionAsync(string secret)
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var eventType = $"dispatch-outcome-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions",
            new
            {
                url = "https://webhook-dispatch-outcome.invalid/receive",
                events = new[] { eventType },
                secret
            });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await createResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task InvokeDispatchAsync(
        WebApplicationFactory<Program> capturingFactory, Guid subscriptionId, string payloadJson)
    {
        using var scope = capturingFactory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // The dispatch job sets its own tenant context internally from the tenantId argument,
        // so no caller-side context plumbing is required here.
        var job = sp.GetRequiredService<WebhookDispatchJob>();
        await job.DispatchAsync(
            subscriptionId,
            TestConstants.RootTenantId,
            $"dispatch-outcome-{Guid.NewGuid():N}",
            payloadJson,
            context: null,
            cancellationToken: CancellationToken.None);
    }

    private static async Task<FSH.Modules.Webhooks.Domain.WebhookDelivery?> ReadLatestDeliveryAsync(
        WebApplicationFactory<Program> capturingFactory, Guid subscriptionId)
    {
        using var scope = capturingFactory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Set the Finbuckle tenant context INLINE: the AsyncLocal mutation must happen in this
        // method body (not a separate awaited helper, or the change doesn't flow back to the
        // caller's execution context) and BEFORE the DbContext is resolved, so its tenant query
        // filter reads a real TenantInfo instead of throwing an NRE on a null one.
        var tenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId).ConfigureAwait(false);
        sp.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var db = sp.GetRequiredService<WebhookDbContext>();
        return await db.Deliveries
            .AsNoTracking()
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.AttemptedAtUtc)
            .FirstOrDefaultAsync();
    }

    private sealed class FixedStatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        public FixedStatusHandler(HttpStatusCode status) => _status = status;

        public bool WasInvoked { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasInvoked = true;
            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }

    #endregion
}
