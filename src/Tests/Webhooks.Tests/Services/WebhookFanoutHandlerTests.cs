using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Domain;
using FSH.Modules.Webhooks.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Webhooks.Tests.Services;

public sealed class WebhookFanoutHandlerTests
{
    private const string TenantId = "tenant-acme";
    private const string EventType = nameof(FakeIntegrationEvent);

    private readonly TestTenantAccessor _tenantAccessor = new();
    private readonly IWebhookDispatcher _dispatcher = Substitute.For<IWebhookDispatcher>();
    private readonly IEventSerializer _serializer = Substitute.For<IEventSerializer>();
    private readonly ILogger<WebhookFanoutHandler<FakeIntegrationEvent>> _logger =
        Substitute.For<ILogger<WebhookFanoutHandler<FakeIntegrationEvent>>>();

    public WebhookFanoutHandlerTests()
    {
        _serializer.Serialize(Arg.Any<IIntegrationEvent>()).Returns("{\"serialized\":true}");
    }

    #region Happy Path

    [Fact]
    public async Task HandleAsync_Should_Enqueue_Delivery_When_Subscription_Matches_Exact_Event()
    {
        await using var db = CreateContext();
        Guid subId = await SeedSubscriptionAsync(db, [EventType], isActive: true);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new FakeIntegrationEvent(TenantId));

        await _dispatcher.Received(1).EnqueueAsync(
            TenantId, subId, EventType, "{\"serialized\":true}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Enqueue_Delivery_When_Subscription_Uses_Wildcard()
    {
        await using var db = CreateContext();
        Guid subId = await SeedSubscriptionAsync(db, ["*"], isActive: true);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new FakeIntegrationEvent(TenantId));

        await _dispatcher.Received(1).EnqueueAsync(
            TenantId, subId, EventType, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Enqueue_For_Each_Matching_Subscription()
    {
        await using var db = CreateContext();
        await SeedSubscriptionAsync(db, [EventType], isActive: true);
        await SeedSubscriptionAsync(db, ["*"], isActive: true);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new FakeIntegrationEvent(TenantId));

        await _dispatcher.Received(2).EnqueueAsync(
            TenantId, Arg.Any<Guid>(), EventType, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Skip / No-Match Branches

    [Fact]
    public async Task HandleAsync_Should_Skip_When_Event_TenantId_Is_Null()
    {
        await using var db = CreateContext();
        await SeedSubscriptionAsync(db, [EventType], isActive: true);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new FakeIntegrationEvent(TenantId: null));

        await _dispatcher.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default, default!, default!, default);
        _serializer.DidNotReceiveWithAnyArgs().Serialize(default!);
    }

    [Fact]
    public async Task HandleAsync_Should_Skip_When_Event_TenantId_Is_Whitespace()
    {
        await using var db = CreateContext();
        await SeedSubscriptionAsync(db, [EventType], isActive: true);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new FakeIntegrationEvent("   "));

        await _dispatcher.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default, default!, default!, default);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Enqueue_When_No_Subscription_Matches_Event()
    {
        await using var db = CreateContext();
        await SeedSubscriptionAsync(db, ["some.other.event"], isActive: true);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new FakeIntegrationEvent(TenantId));

        await _dispatcher.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default, default!, default!, default);
        _serializer.DidNotReceiveWithAnyArgs().Serialize(default!);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Enqueue_When_Matching_Subscription_Is_Inactive()
    {
        await using var db = CreateContext();
        await SeedSubscriptionAsync(db, [EventType], isActive: false);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new FakeIntegrationEvent(TenantId));

        await _dispatcher.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default, default!, default!, default);
    }

    #endregion

    #region Resilience

    [Fact]
    public async Task HandleAsync_Should_Continue_Fanout_When_One_Enqueue_Throws()
    {
        await using var db = CreateContext();
        Guid first = await SeedSubscriptionAsync(db, [EventType], isActive: true);
        Guid second = await SeedSubscriptionAsync(db, [EventType], isActive: true);

        _dispatcher
            .EnqueueAsync(TenantId, first, EventType, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("transient enqueue failure"));

        var handler = CreateHandler(db);

        // Must not bubble — a single bad subscription cannot abort fan-out to the rest.
        await Should.NotThrowAsync(async () => await handler.HandleAsync(new FakeIntegrationEvent(TenantId)));

        await _dispatcher.Received(1).EnqueueAsync(
            TenantId, second, EventType, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_When_Event_Is_Null()
    {
        await using var db = CreateContext();
        var handler = CreateHandler(db);

        await Should.ThrowAsync<ArgumentNullException>(async () => await handler.HandleAsync(null!));
    }

    #endregion

    #region Helpers

    private WebhookFanoutHandler<FakeIntegrationEvent> CreateHandler(WebhookDbContext db) =>
        new(db, _dispatcher, _serializer, _tenantAccessor, _logger);

    private WebhookDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseInMemoryDatabase($"webhooks-{Guid.NewGuid():N}")
            .Options;

        var settings = Options.Create(new DatabaseOptions());
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");

        return new WebhookDbContext(_tenantAccessor, options, settings, environment);
    }

    private async Task<Guid> SeedSubscriptionAsync(WebhookDbContext db, string[] events, bool isActive)
    {
        // Install tenant context so Finbuckle stamps the seeded row (Overwrite mode)
        // and the handler's tenant-filtered read can see it.
        SetTenant(TenantId);

        WebhookSubscription sub = WebhookSubscription.Create("https://example.com/hook", events, "hash");
        if (!isActive)
        {
            sub.Deactivate();
        }

        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return sub.Id;
    }

    private void SetTenant(string tenantId) =>
        ((IMultiTenantContextSetter)_tenantAccessor).MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(tenantId, tenantId));

    #endregion

    private sealed class TestTenantAccessor : IMultiTenantContextAccessor<AppTenantInfo>, IMultiTenantContextSetter
    {
        private IMultiTenantContext<AppTenantInfo> _context = new MultiTenantContext<AppTenantInfo>(new AppTenantInfo());

        public IMultiTenantContext<AppTenantInfo> MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextSetter.MultiTenantContext
        {
            set => _context = (IMultiTenantContext<AppTenantInfo>)value;
        }
    }
}

public sealed record FakeIntegrationEvent(string? TenantId) : IIntegrationEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public string CorrelationId { get; } = Guid.CreateVersion7().ToString();
    public string Source { get; } = "Tests";
}
