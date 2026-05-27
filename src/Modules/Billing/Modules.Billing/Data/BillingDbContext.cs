using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Data;

/// <summary>
/// Billing data lives in the main application database rather than per-tenant databases because
/// invoices and subscriptions are an administrative concern that needs cross-tenant visibility.
/// Tenant ownership is represented as an explicit <c>TenantId</c> column and filtered in query
/// services.
/// </summary>
public sealed class BillingDbContext : DbContext
{
    public const string Schema = "billing";

    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    public DbSet<BillingPlan> Plans => Set<BillingPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<UsageSnapshot> UsageSnapshots => Set<UsageSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
