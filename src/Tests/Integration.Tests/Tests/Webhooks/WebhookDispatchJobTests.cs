using System.Reflection;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Services;
using Hangfire;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable CA1707 // Test method names use underscores by convention

namespace Integration.Tests.Tests.Webhooks;

[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookDispatchJobTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public WebhookDispatchJobTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public void DispatchAsync_Should_BeAnnotated_WithAutomaticRetry_ExponentialBackoff()
    {
        var method = typeof(WebhookDispatchJob).GetMethod(
            nameof(WebhookDispatchJob.DispatchAsync),
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();

        var retry = method!.GetCustomAttribute<AutomaticRetryAttribute>();
        retry.ShouldNotBeNull("WebhookDispatchJob.DispatchAsync must be decorated with [AutomaticRetry] so Hangfire reschedules failed deliveries.");

        retry!.Attempts.ShouldBe(4);
        retry.OnAttemptsExceeded.ShouldBe(AttemptsExceededAction.Fail);

        var delaysField = typeof(AutomaticRetryAttribute).GetField(
            "_delaysInSeconds",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (delaysField is not null)
        {
            var delays = (int[]?)delaysField.GetValue(retry);
            delays.ShouldNotBeNull();
            delays!.Length.ShouldBeGreaterThan(0);
            // Verify backoff actually grows (exponential-ish).
            for (int i = 1; i < delays.Length; i++)
            {
                delays[i].ShouldBeGreaterThan(delays[i - 1]);
            }
        }
    }

    [Fact]
    public async Task DispatchAsync_Should_PersistDeliveryRow_And_ThrowOnTransientFailure()
    {
        _ = _factory.Server;
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        var uniqueEvent = $"dispatch-test-{Guid.NewGuid():N}";
        var createResponse = await rootClient.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions",
            new
            {
                url = "https://127.0.0.1:1/dispatch-test", // unreachable → transient failure
                events = new[] { uniqueEvent },
                secret = "dispatch-secret"
            });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var subscriptionId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<WebhookDispatchJob>();

        // Invoke the job directly (bypassing Hangfire's scheduler). The job sets its own
        // tenant context from the tenantId parameter, so no caller-side context plumbing.
        await Should.ThrowAsync<WebhookDeliveryFailedException>(() =>
            job.DispatchAsync(
                subscriptionId,
                TestConstants.RootTenantId,
                uniqueEvent,
                "{\"id\":\"abc\"}",
                context: null,
                cancellationToken: CancellationToken.None));

        // The job must have persisted a delivery row tagged with the unique event type.
        using var readScope = _factory.Services.CreateScope();
        var tenant = await readScope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        readScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var db = readScope.ServiceProvider.GetRequiredService<WebhookDbContext>();
        var delivery = await db.Deliveries
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.SubscriptionId == subscriptionId && d.EventType == uniqueEvent);

        delivery.ShouldNotBeNull("Dispatch job must persist a WebhookDelivery row even when the attempt fails.");
        delivery!.Success.ShouldBeFalse();
        delivery.AttemptCount.ShouldBe(1);
        delivery.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DispatchAsync_Should_CompleteSilently_When_SubscriptionInactiveOrMissing()
    {
        _ = _factory.Server;
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<WebhookDispatchJob>();

        // Unknown subscription — job must NOT throw (avoids Hangfire retry loop on a
        // permanent condition).
        await job.DispatchAsync(
            Guid.NewGuid(),
            TestConstants.RootTenantId,
            "noop",
            "{}",
            context: null,
            cancellationToken: CancellationToken.None);
    }

    [Fact]
    public async Task EnqueueAsync_Should_ThrowOnWhitespaceTenantId()
    {
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

        await Should.ThrowAsync<ArgumentException>(() =>
            dispatcher.EnqueueAsync(" ", Guid.NewGuid(), "user.created", "{}"));
    }

    [Fact]
    public async Task EnqueueAsync_Should_ThrowOnWhitespaceEventType()
    {
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

        await Should.ThrowAsync<ArgumentException>(() =>
            dispatcher.EnqueueAsync(TestConstants.RootTenantId, Guid.NewGuid(), "  ", "{}"));
    }

    [Fact]
    public async Task EnqueueAsync_Should_ThrowOnWhitespacePayload()
    {
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

        await Should.ThrowAsync<ArgumentException>(() =>
            dispatcher.EnqueueAsync(TestConstants.RootTenantId, Guid.NewGuid(), "user.created", "  "));
    }
}
