using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Eventing.Persistence;
using FSH.Framework.Shared.Multitenancy;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Eventing;

/// <summary>
/// Covers audit finding REL-02: a failed outbox message was retried every dispatch cycle with no
/// backoff, then dead-lettered with no way to recover it. Exercises the real
/// <see cref="EfCoreOutboxStore"/> over Postgres.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class OutboxRetryTests
{
    private readonly FshWebApplicationFactory _factory;

    public OutboxRetryTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MarkAsFailed_NotDead_Should_BackOff_And_ExcludeFromPendingUntilDue()
    {
        using var scope = await CreateTenantScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<EventingDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var message = NewMessage();
        db.Set<OutboxMessage>().Add(message);
        await db.SaveChangesAsync();

        await store.MarkAsFailedAsync(message, "transient boom", isDead: false);

        message.NextRetryAt.ShouldNotBeNull("a non-dead failure must schedule a backed-off retry");
        message.NextRetryAt.Value.ShouldBeGreaterThan(
            DateTime.UtcNow.AddSeconds(20),
            "the first retry backs off by the base delay (30s), not the next 10s cycle");

        var pending = await ClaimAsync(store);
        pending.ShouldNotContain(
            m => m.Id == message.Id,
            "a message whose NextRetryAt is in the future must be excluded from the dispatch batch");
    }

    [Fact]
    public async Task RedriveDeadLetters_Should_ResetDeadMessage_And_MakeItPendingAgain()
    {
        using var scope = await CreateTenantScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<EventingDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var message = NewMessage();
        message.IsDead = true;
        message.RetryCount = 5;
        message.LastError = "exhausted";
        db.Set<OutboxMessage>().Add(message);
        await db.SaveChangesAsync();

        var redriven = await store.RedriveDeadLettersAsync([message.Id]);

        redriven.ShouldBe(1);
        (await store.GetDeadLetteredAsync(500)).ShouldNotContain(m => m.Id == message.Id);
        (await ClaimAsync(store)).ShouldContain(
            m => m.Id == message.Id,
            "a redriven message clears IsDead/RetryCount/NextRetryAt and becomes eligible again");
    }

    private static Task<IReadOnlyList<OutboxMessage>> ClaimAsync(IOutboxStore store) =>
        store.ClaimBatchAsync(500, "OutboxRetryTests", TimeSpan.FromMinutes(5));

    private static OutboxMessage NewMessage() => new()
    {
        Id = Guid.NewGuid(),
        // Old timestamp so it sorts first in the CreatedOnUtc-ascending pending batch.
        CreatedOnUtc = DateTime.UtcNow.AddDays(-1),
        Type = "REL-02.OutboxRetryTest",
        Payload = "{}",
        TenantId = TestConstants.RootTenantId,
        RetryCount = 0,
        IsDead = false,
    };

    private async Task<IServiceScope> CreateTenantScopeAsync()
    {
        var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        return scope;
    }
}
