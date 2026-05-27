using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.PlanId).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.TenantId)
            .HasFilter($"\"Status\" = {(int)SubscriptionStatus.Active}")
            .IsUnique()
            .HasDatabaseName("ux_subscriptions_tenantid_active");

        builder.Ignore(x => x.DomainEvents);
    }
}
