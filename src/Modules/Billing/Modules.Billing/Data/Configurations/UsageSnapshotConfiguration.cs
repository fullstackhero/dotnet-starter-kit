using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class UsageSnapshotConfiguration : IEntityTypeConfiguration<UsageSnapshot>
{
    public void Configure(EntityTypeBuilder<UsageSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UsageSnapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Resource).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.PeriodYear, x.PeriodMonth, x.Resource })
            .IsUnique()
            .HasDatabaseName("ux_usage_snapshots_tenant_period_resource");

        builder.Ignore(x => x.DomainEvents);
    }
}
