using FSH.Starter.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<ChatHistory> ChatHistories => Set<ChatHistory>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Tenant>().ToTable("Tenants");
        modelBuilder.Entity<Faq>().ToTable("Faqs");
        modelBuilder.Entity<ChatHistory>().ToTable("ChatHistories");
        modelBuilder.Entity<Subscription>().ToTable("Subscriptions");

        modelBuilder.Entity<Faq>().HasIndex(f => new { f.TenantId, f.Question });
        modelBuilder.Entity<Subscription>().HasIndex(s => s.TenantId);
    }
}
