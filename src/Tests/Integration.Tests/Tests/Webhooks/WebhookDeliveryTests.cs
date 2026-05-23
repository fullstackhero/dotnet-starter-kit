using System.Net.Http.Headers;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Webhooks.Contracts.Dtos;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Services;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
#pragma warning disable CA1707 // Test method names use underscores by convention

namespace Integration.Tests.Tests.Webhooks;

/// <summary>
/// Exercises the delivery-log read path (<c>GET /subscriptions/{id}/deliveries</c> →
/// <see cref="FSH.Modules.Webhooks.Features.v1.GetWebhookDeliveries.GetWebhookDeliveriesQueryHandler"/>),
/// the synchronous test-send flow
/// (<c>POST /subscriptions/{id}/test</c> →
/// <see cref="FSH.Modules.Webhooks.Features.v1.TestWebhookSubscription.TestWebhookSubscriptionCommandHandler"/>
/// → <see cref="WebhookDeliveryService"/>), and tenant isolation of the delivery rows.
///
/// To make the test-send actually produce a delivery row WITHOUT a real network call, we swap the
/// primary transport of the named "Webhooks" HttpClient for a recorder on a per-test derived factory
/// (the same DI-override technique used by WebhookSignatureTests). The recorder lets us steer the
/// HTTP status code so we can cover both the success row and the failure row branches of
/// <see cref="WebhookDeliveryService"/>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookDeliveryTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public WebhookDeliveryTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task GetDeliveries_Should_ReturnRecordedDelivery_When_TestSendSucceeded()
    {
        // Arrange — a capturing factory whose "Webhooks" client returns 200 for the test-send.
        var transport = new SteerableHandler(HttpStatusCode.OK);
        await using var capturingFactory = CreateCapturingFactory(transport);
        using var client = await CreateRootClientFor(capturingFactory);

        var (subscriptionId, _) = await CreateSubscriptionAsync(client, secret: "deliveries-secret");

        // Act — fire the test-send; the handler delivers synchronously and persists a row.
        var testResponse = await client.PostAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{subscriptionId}/test", content: null);
        testResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        transport.WasInvoked.ShouldBeTrue("Test-send must perform an outbound HTTP POST.");

        var deliveries = await GetDeliveriesAsync(client, subscriptionId);

        // Assert
        deliveries.TotalCount.ShouldBe(1);
        var row = deliveries.Items.ShouldHaveSingleItem();
        row.SubscriptionId.ShouldBe(subscriptionId);
        row.EventType.ShouldBe("webhook.test");
        row.Success.ShouldBeTrue();
        row.HttpStatusCode.ShouldBe(200);
        row.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetDeliveries_Should_RecordFailureRow_When_RemoteReturnsError()
    {
        // Arrange — the recorder returns 500 so DeliverAsync records a non-success row.
        var transport = new SteerableHandler(HttpStatusCode.InternalServerError);
        await using var capturingFactory = CreateCapturingFactory(transport);
        using var client = await CreateRootClientFor(capturingFactory);

        var (subscriptionId, _) = await CreateSubscriptionAsync(client, secret: "fail-secret");

        // Act
        var testResponse = await client.PostAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{subscriptionId}/test", content: null);
        testResponse.StatusCode.ShouldBe(HttpStatusCode.OK); // test-send itself succeeds; delivery failed

        var deliveries = await GetDeliveriesAsync(client, subscriptionId);

        // Assert — the delivery service records the failed attempt rather than throwing.
        var row = deliveries.Items.ShouldHaveSingleItem();
        row.Success.ShouldBeFalse();
        row.HttpStatusCode.ShouldBe(500);
    }

    [Fact]
    public async Task TestSend_Should_AttachSignatureHeader_When_SecretConfigured()
    {
        // Arrange
        const string secret = "test-send-sig-secret";
        var transport = new SteerableHandler(HttpStatusCode.OK);
        await using var capturingFactory = CreateCapturingFactory(transport);
        using var client = await CreateRootClientFor(capturingFactory);

        var (subscriptionId, _) = await CreateSubscriptionAsync(client, secret);

        // Act
        await client.PostAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{subscriptionId}/test", content: null);

        // Assert — DeliverAsync set the HMAC + event headers on the request content.
        transport.CapturedHeaders.ShouldContainKey("X-Webhook-Signature");
        transport.CapturedHeaders["X-Webhook-Signature"].ShouldStartWith("sha256=");
        transport.CapturedHeaders.ShouldContainKey("X-Webhook-Event");
        transport.CapturedHeaders["X-Webhook-Event"].ShouldBe("webhook.test");
        transport.CapturedHeaders.ShouldContainKey("X-Webhook-Delivery-Id");
    }

    [Fact]
    public async Task GetDeliveries_Should_Paginate_When_MultipleDeliveriesExist()
    {
        // Arrange — produce 3 deliveries by sending the test event 3 times.
        var transport = new SteerableHandler(HttpStatusCode.OK);
        await using var capturingFactory = CreateCapturingFactory(transport);
        using var client = await CreateRootClientFor(capturingFactory);

        var (subscriptionId, _) = await CreateSubscriptionAsync(client, secret: "page-secret");

        for (int i = 0; i < 3; i++)
        {
            var r = await client.PostAsync(
                $"{TestConstants.WebhooksBasePath}/subscriptions/{subscriptionId}/test", content: null);
            r.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        // Act — page size 2 → page 1 has 2 rows, page 2 has 1 row, totalCount = 3, totalPages = 2.
        var page1 = await GetDeliveriesAsync(client, subscriptionId, pageNumber: 1, pageSize: 2);
        var page2 = await GetDeliveriesAsync(client, subscriptionId, pageNumber: 2, pageSize: 2);

        // Assert
        page1.TotalCount.ShouldBe(3);
        page1.TotalPages.ShouldBe(2);
        page1.PageNumber.ShouldBe(1);
        page1.Items.Count.ShouldBe(2);
        page1.HasNext.ShouldBeTrue();
        page1.HasPrevious.ShouldBeFalse();

        page2.Items.Count.ShouldBe(1);
        page2.HasNext.ShouldBeFalse();
        page2.HasPrevious.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDeliveries_Should_ReturnEmpty_When_NoDeliveriesYet()
    {
        // Arrange — a fresh subscription with no test-sends.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (subscriptionId, _) = await CreateSubscriptionAsync(client, secret: "empty-secret");

        // Act
        var deliveries = await GetDeliveriesAsync(client, subscriptionId);

        // Assert
        deliveries.TotalCount.ShouldBe(0);
        deliveries.Items.ShouldBeEmpty();
        deliveries.TotalPages.ShouldBe(0);
    }

    #endregion

    #region Exception / Authz

    [Fact]
    public async Task TestSend_Should_Return404_When_SubscriptionDoesNotExist()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act — the handler throws NotFoundException, mapped to 404.
        var response = await client.PostAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{Guid.NewGuid()}/test", content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDeliveries_Should_Return401_When_NotAuthenticated()
    {
        // Arrange — no bearer token.
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{Guid.NewGuid()}/deliveries?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDeliveries_Should_NotLeakOtherTenantsDeliveries_When_QueriedCrossTenant()
    {
        // Arrange — root creates a subscription and a (successful) delivery via test-send.
        var transport = new SteerableHandler(HttpStatusCode.OK);
        await using var capturingFactory = CreateCapturingFactory(transport);
        using var rootClient = await CreateRootClientFor(capturingFactory);

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var (rootSubId, _) = await CreateSubscriptionAsync(rootClient, secret: $"iso-secret-{uniqueId}");
        var testResponse = await rootClient.PostAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{rootSubId}/test", content: null);
        testResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Confirm the row exists for root (sanity).
        var rootDeliveries = await GetDeliveriesAsync(rootClient, rootSubId);
        rootDeliveries.TotalCount.ShouldBe(1);

        // Provision a second tenant and authenticate as its admin.
        var otherTenantId = $"wh-deliv-iso-{uniqueId}";
        var otherAdminEmail = $"wh-deliv-iso-{uniqueId}@tenant.com";
        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            capturingFactory, otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act — the other tenant queries root's subscription id. The Deliveries table is
        // IsMultiTenant(), so the Finbuckle filter scopes the read to the other tenant → empty.
        var crossDeliveries = await GetDeliveriesAsync(otherClient, rootSubId);

        // Assert — no leak.
        crossDeliveries.TotalCount.ShouldBe(0);
        crossDeliveries.Items.ShouldBeEmpty();
    }

    #endregion

    #region Direct service coverage

    [Fact]
    public async Task DeliverAsync_Should_RecordRowWithoutSignature_When_NoSecretConfigured()
    {
        // Arrange — call the delivery service directly (no subscription secret) to cover the
        // "no signature header" branch of WebhookDeliveryService.
        var transport = new SteerableHandler(HttpStatusCode.OK);
        await using var capturingFactory = CreateCapturingFactory(transport);

        // Need a real subscription id so the delivery row's FK/tenant scoping is consistent.
        using var client = await CreateRootClientFor(capturingFactory);
        var (subscriptionId, _) = await CreateSubscriptionAsync(client, secret: null);

        using var scope = capturingFactory.Services.CreateScope();
        SetRootTenantContext(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();

        // Act
        await service.DeliverAsync(
            subscriptionId,
            "https://no-secret.invalid/hook",
            secretHash: null,
            eventType: "manual.event",
            payloadJson: "{\"manual\":true}",
            ct: CancellationToken.None);

        // Assert — no signature header was sent (secret was null) but the event header still is.
        transport.WasInvoked.ShouldBeTrue();
        transport.CapturedHeaders.ShouldNotContainKey("X-Webhook-Signature");
        transport.CapturedHeaders.ShouldContainKey("X-Webhook-Event");

        var deliveries = await GetDeliveriesAsync(client, subscriptionId);
        var row = deliveries.Items.ShouldHaveSingleItem();
        row.EventType.ShouldBe("manual.event");
        row.Success.ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private WebApplicationFactory<Program> CreateCapturingFactory(SteerableHandler transport) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient("Webhooks")
                    .ConfigurePrimaryHttpMessageHandler(() => transport);
            });
        });

    private async Task<HttpClient> CreateRootClientFor(WebApplicationFactory<Program> capturingFactory)
    {
        // Auth uses the shared factory's token-issue endpoint; the returned JWT is valid against
        // the derived factory too (same signing key/host configuration).
        var token = await _auth.GetRootAdminTokenAsync();
        var client = capturingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        return client;
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        WebApplicationFactory<Program> capturingFactory,
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var token = await _auth.GetTokenAsync(email, password, tenant);
                var client = capturingFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                client.DefaultRequestHeaders.Add("tenant", tenant);
                return client;
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException($"Could not authenticate {email} for tenant {tenant}.");
    }

    private static async Task<(Guid SubscriptionId, string EventType)> CreateSubscriptionAsync(
        HttpClient client, string? secret)
    {
        var eventType = $"deliv-test-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions",
            new
            {
                url = "https://webhook-delivery-test.invalid/receive",
                events = new[] { eventType },
                secret
            });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var subscriptionId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        return (subscriptionId, eventType);
    }

    private static async Task<PagedResponse<WebhookDeliveryDto>> GetDeliveriesAsync(
        HttpClient client, Guid subscriptionId, int pageNumber = 1, int pageSize = 10)
    {
        var response = await client.GetAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{subscriptionId}/deliveries?pageNumber={pageNumber}&pageSize={pageSize}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<WebhookDeliveryDto>>();
        result.ShouldNotBeNull();
        return result!;
    }

    private static void SetRootTenantContext(IServiceProvider sp)
    {
        var tenant = sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId).GetAwaiter().GetResult();
        sp.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds.");
    }

    /// <summary>
    /// Test transport for the named "Webhooks" HttpClient. Returns a steerable status code and records
    /// the outbound request's content headers (signature/event/delivery-id are set on the CONTENT).
    /// </summary>
    private sealed class SteerableHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        public SteerableHandler(HttpStatusCode status) => _status = status;

        public bool WasInvoked { get; private set; }

        public Dictionary<string, string> CapturedHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasInvoked = true;

            if (request.Content is not null)
            {
                // Force materialization so content headers are populated.
                _ = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                foreach (var header in request.Content.Headers)
                {
                    CapturedHeaders[header.Key] = string.Join(",", header.Value);
                }
            }

            foreach (var header in request.Headers)
            {
                CapturedHeaders[header.Key] = string.Join(",", header.Value);
            }

            return new HttpResponseMessage(_status);
        }
    }

    #endregion
}
