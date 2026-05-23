using System.Security.Cryptography;
using System.Text;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Webhooks.Services;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
#pragma warning disable CA1707 // Test method names use underscores by convention

namespace Integration.Tests.Tests.Webhooks;

/// <summary>
/// Proves the security-load-bearing HMAC signature that <see cref="WebhookDispatchJob"/> attaches
/// to every outbound delivery. The production signer (<see cref="WebhookPayloadSigner"/>) computes
/// <c>sha256={lowercase-hex(HMACSHA256(UTF8(secret), UTF8(payload)))}</c> and sets it on the
/// <c>X-Webhook-Signature</c> header.
///
/// We do NOT trust the delivery row (it never stores the header). Instead we capture the *live*
/// outbound HTTP request by swapping the primary transport of the named "Webhooks" HttpClient for a
/// recording handler — a pure test-side DI override via <c>WithWebHostBuilder</c>, no production seam
/// added. The resilience pipeline registered by the module stays intact; only the socket transport is
/// replaced. We then recompute HMAC over the EXACT bytes the server transmitted and compare.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookSignatureTests
{
    private const string SignatureHeader = "X-Webhook-Signature";
    private const string EventHeader = "X-Webhook-Event";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public WebhookSignatureTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task DispatchAsync_Should_SendSignatureThatVerifiesAgainstSecret_When_DeliverySucceeds()
    {
        // Arrange
        const string secret = "sig-test-secret-7f3a";
        var payloadJson = $"{{\"id\":\"{Guid.NewGuid():N}\",\"value\":42}}";
        var capture = new RequestCapture();

        await using var capturingFactory = CreateCapturingFactory(capture);
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        var (subscriptionId, eventType) = await CreateSubscriptionAsync(rootClient, secret);

        // Act — invoke the dispatch job directly so it performs a real (captured) HTTP POST.
        // The recording handler returns 200, so the success path runs and the job does not throw.
        await InvokeDispatchAsync(capturingFactory, subscriptionId, eventType, payloadJson);

        // Assert — the request was actually sent and carried the signature header.
        capture.WasInvoked.ShouldBeTrue("The dispatch job must perform an outbound HTTP POST.");
        capture.Headers.ShouldContainKey(SignatureHeader);
        var signature = capture.Headers[SignatureHeader];

        // The signature must verify: recompute HMAC-SHA256 over the EXACT bytes the server sent,
        // keyed with the subscription secret, and compare to the transmitted header.
        var expected = ComputeExpectedSignature(capture.BodyBytes!, secret);
        signature.ShouldBe(expected);

        // And it must verify against the literal payload bytes we asked to be delivered
        // (proves the body was not mangled in transit).
        var expectedFromSourcePayload = ComputeExpectedSignature(Encoding.UTF8.GetBytes(payloadJson), secret);
        signature.ShouldBe(expectedFromSourcePayload);
    }

    [Fact]
    public async Task DispatchAsync_Should_UseSha256LowercaseHexFormat_When_SecretIsConfigured()
    {
        // Arrange
        const string secret = "format-secret-c1d2";
        var payloadJson = "{\"event\":\"format-check\"}";
        var capture = new RequestCapture();

        await using var capturingFactory = CreateCapturingFactory(capture);
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        var (subscriptionId, eventType) = await CreateSubscriptionAsync(rootClient, secret);

        // Act
        await InvokeDispatchAsync(capturingFactory, subscriptionId, eventType, payloadJson);

        // Assert — exact wire format: "sha256=" prefix + 64 lowercase hex chars (32-byte digest).
        var signature = capture.Headers[SignatureHeader];
        signature.ShouldStartWith("sha256=");
        var hex = signature["sha256=".Length..];
        hex.Length.ShouldBe(64);
        // Lowercase hex only: no uppercase A–F, every char a hex digit.
        hex.ShouldAllBe(c => Uri.IsHexDigit(c) && !char.IsUpper(c));

        // The captured event header must also match what we dispatched (sanity on the captured request).
        capture.Headers.ShouldContainKey(EventHeader);
        capture.Headers[EventHeader].ShouldBe(eventType);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DispatchAsync_Should_ProduceSignatureThatRejectsTamperedPayload_When_Verified()
    {
        // Arrange — negative control: a verifier using the wrong payload (or wrong secret) must NOT match.
        const string secret = "tamper-secret-ab12";
        var payloadJson = "{\"amount\":100,\"to\":\"alice\"}";
        var capture = new RequestCapture();

        await using var capturingFactory = CreateCapturingFactory(capture);
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        var (subscriptionId, eventType) = await CreateSubscriptionAsync(rootClient, secret);

        // Act
        await InvokeDispatchAsync(capturingFactory, subscriptionId, eventType, payloadJson);
        var signature = capture.Headers[SignatureHeader];

        // Assert — HMAC over a tampered payload (single byte changed) does not match the header.
        var tampered = Encoding.UTF8.GetBytes("{\"amount\":900,\"to\":\"alice\"}");
        var tamperedSignature = ComputeExpectedSignature(tampered, secret);
        tamperedSignature.ShouldNotBe(signature);

        // HMAC keyed with the wrong secret also does not match — the signature is secret-bound.
        var wrongSecretSignature = ComputeExpectedSignature(capture.BodyBytes!, "not-the-secret");
        wrongSecretSignature.ShouldNotBe(signature);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Builds a derived factory whose "Webhooks" named HttpClient routes through a recording transport
    /// instead of a real socket. This is a test-only DI override layered over the shared host; it reuses
    /// the same Postgres container/config (so the seeded root tenant and migrated webhook schema are
    /// visible). No production code or seam is touched.
    /// </summary>
    private WebApplicationFactory<Program> CreateCapturingFactory(RequestCapture capture) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient("Webhooks")
                    .ConfigurePrimaryHttpMessageHandler(() => new RecordingHandler(capture));
            });
        });

    private static async Task<(Guid SubscriptionId, string EventType)> CreateSubscriptionAsync(
        HttpClient rootClient, string secret)
    {
        var eventType = $"sig-test-{Guid.NewGuid():N}";
        var createResponse = await rootClient.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions",
            new
            {
                url = "https://webhook-sig-test.invalid/receive",
                events = new[] { eventType },
                secret
            });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var subscriptionId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        return (subscriptionId, eventType);
    }

    private static async Task InvokeDispatchAsync(
        WebApplicationFactory<Program> capturingFactory,
        Guid subscriptionId,
        string eventType,
        string payloadJson)
    {
        using var scope = capturingFactory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Set the Finbuckle tenant context INLINE in this scope (AsyncLocal — must not cross an awaited
        // helper boundary or the DbContext tenant filter sees a null TenantInfo). The dispatch job sets
        // its own context internally too, but it resolves a fresh inner scope, so this is for safety on
        // the calling scope.
        var tenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        sp.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var job = sp.GetRequiredService<WebhookDispatchJob>();
        await job.DispatchAsync(
            subscriptionId,
            TestConstants.RootTenantId,
            eventType,
            payloadJson,
            context: null,
            cancellationToken: CancellationToken.None);
    }

    /// <summary>
    /// Recomputes the production signature: <c>sha256={lowercase-hex(HMACSHA256(UTF8(secret), bytes))}</c>.
    /// Mirrors <see cref="WebhookPayloadSigner.Sign"/> but is implemented independently here so the test
    /// is an external verifier, not a tautology against the production method.
    /// </summary>
    private static string ComputeExpectedSignature(byte[] payloadBytes, string secret)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payloadBytes);
        // ToLowerInvariant is required to match the production wire format exactly (WebhookPayloadSigner).
#pragma warning disable CA1308
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
#pragma warning restore CA1308
    }

    private sealed class RequestCapture
    {
        public bool WasInvoked { get; set; }
        public byte[]? BodyBytes { get; set; }
        public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly RequestCapture _capture;

        public RecordingHandler(RequestCapture capture) => _capture = capture;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _capture.WasInvoked = true;

            if (request.Content is not null)
            {
                _capture.BodyBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Signature/event/delivery headers are set on the request CONTENT, not the request.
                foreach (var header in request.Content.Headers)
                {
                    _capture.Headers[header.Key] = string.Join(",", header.Value);
                }
            }

            foreach (var header in request.Headers)
            {
                _capture.Headers[header.Key] = string.Join(",", header.Value);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    #endregion
}
