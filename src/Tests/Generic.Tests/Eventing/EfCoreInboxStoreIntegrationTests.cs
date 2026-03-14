using Finbuckle.MultiTenant;
using FSH.Framework.Eventing.Inbox;
using FSH.Framework.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using NSubstitute;

namespace Generic.Tests.Eventing;

public class TestEventingDbContext : DbContext
{
    public TestEventingDbContext(DbContextOptions<TestEventingDbContext> options) : base(options) { }

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("InboxMessages");
            builder.HasKey(x => new { x.Id, x.HandlerName });
            builder.Property(x => x.TenantId).HasMaxLength(64);
        });
    }
}

public class EfCoreInboxStoreIntegrationTests
{
    [Fact]
    public async Task HasProcessedAsync_ShouldReturnTrue_OnlyForMatchingTenantId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestEventingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new TestEventingDbContext(options);
        var store = new EfCoreInboxStore<TestEventingDbContext>(dbContext);

        var eventId = Guid.NewGuid();
        var handlerName = "TestHandler";
        var tenantId1 = "tenant-1";
        var tenantId2 = "tenant-2";

        var message = new InboxMessage
        {
            Id = eventId,
            HandlerName = handlerName,
            EventType = "TestType",
            ProcessedOnUtc = DateTime.UtcNow,
            TenantId = tenantId1
        };

        dbContext.InboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        // Act
        // EVENTING-2: HasProcessedAsync should take tenantId and verify it correctly isolates checks
        var processedTenant1 = await store.HasProcessedAsync(eventId, handlerName, tenantId1);
        var processedTenant2 = await store.HasProcessedAsync(eventId, handlerName, tenantId2);
        
        // Root tenant fallback check (null/root)
        var processedRoot = await store.HasProcessedAsync(eventId, handlerName, null);

        // Assert
        processedTenant1.ShouldBeTrue();
        processedTenant2.ShouldBeFalse();
        processedRoot.ShouldBeFalse();
    }
}
