using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Webhooks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Webhooks.Data;

public sealed class WebhookDbContext(
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    DbContextOptions<WebhookDbContext> options,
    IOptions<DatabaseOptions> settings,
    IHostEnvironment environment) : BaseDbContext(multiTenantContextAccessor, options, settings, environment)
{
    public DbSet<WebhookSubscription> Subscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> Deliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("webhooks");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhookDbContext).Assembly);
    }
}
