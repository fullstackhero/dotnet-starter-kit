using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auditing.Tests.Persistence;

public sealed class AuditingSaveChangesInterceptorTests
{
    [Fact]
    public async Task SavingChanges_Should_MaskSensitiveValues_When_EntityIsInserted()
    {
        // Arrange
        var publisher = new CapturingPublisher();
        await using var db = CreateContext(publisher);
        db.Accounts.Add(new Account { Id = 1, Email = "a@b.com", PasswordHash = "hash-value", RefreshToken = "refresh-value" });

        // Act
        await db.SaveChangesAsync();

        // Assert
        var changes = publisher.ChangesFor(nameof(Account));
        changes.Single(c => c.Name == nameof(Account.Email)).NewValue.ShouldBe("a@b.com");
        AssertMasked(changes.Single(c => c.Name == nameof(Account.PasswordHash)));
        AssertMasked(changes.Single(c => c.Name == nameof(Account.RefreshToken)));
    }

    [Fact]
    public async Task SavingChanges_Should_MaskOldAndNewValues_When_SensitivePropertyIsUpdated()
    {
        // Arrange
        var publisher = new CapturingPublisher();
        await using var db = CreateContext(publisher);
        var account = new Account { Id = 1, Email = "a@b.com", PasswordHash = "old-hash", RefreshToken = null };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        publisher.Events.Clear();

        // Act
        account.PasswordHash = "new-hash";
        await db.SaveChangesAsync();

        // Assert
        var change = publisher.ChangesFor(nameof(Account)).Single(c => c.Name == nameof(Account.PasswordHash));
        change.IsSensitive.ShouldBeTrue();
        change.OldValue.ShouldBe("****");
        change.NewValue.ShouldBe("****");
    }

    [Fact]
    public async Task SavingChanges_Should_SkipEntity_When_EntityIsAuditExempt()
    {
        // Arrange
        var publisher = new CapturingPublisher();
        await using var db = CreateContext(publisher);
        db.Reports.Add(new WhistleblowerReport { Id = 1, Body = "confidential report" });
        db.Accounts.Add(new Account { Id = 1, Email = "a@b.com", PasswordHash = "x" });

        // Act
        await db.SaveChangesAsync();

        // Assert — the non-exempt entity is audited (positive control), the exempt one never is.
        publisher.ChangesFor(nameof(Account)).ShouldNotBeEmpty();
        publisher.ChangesFor(nameof(WhistleblowerReport)).ShouldBeEmpty();
    }

    private static void AssertMasked(PropertyChange change)
    {
        change.IsSensitive.ShouldBeTrue();
        change.OldValue.ShouldBeNull();
        change.NewValue.ShouldBe("****");
    }

    private static TestDbContext CreateContext(IAuditPublisher publisher)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditingSaveChangesInterceptor(publisher, TimeProvider.System))
            .Options;
        return new TestDbContext(options);
    }

    private sealed class Account
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string? PasswordHash { get; set; }
        public string? RefreshToken { get; set; }
    }

    private sealed class WhistleblowerReport : IAuditExempt
    {
        public int Id { get; set; }
        public string Body { get; set; } = default!;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<WhistleblowerReport> Reports => Set<WhistleblowerReport>();
    }

    private sealed class CapturingPublisher : IAuditPublisher
    {
        public List<IAuditEvent> Events { get; } = [];

        public IAuditScope CurrentScope => throw new NotSupportedException();

        public ValueTask PublishAsync(IAuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }

        public List<PropertyChange> ChangesFor(string entityName) =>
            Events.Select(e => e.Payload)
                .OfType<EntityChangeEventPayload>()
                .Where(p => p.EntityName == entityName)
                .SelectMany(p => p.Changes)
                .ToList();
    }
}
