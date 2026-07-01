using System.Text.Json;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class BillingPlanConfiguration : IEntityTypeConfiguration<BillingPlan>
{
    public void Configure(EntityTypeBuilder<BillingPlan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Ignore(x => x.Currency);
        builder.OwnsOne(x => x.MonthlyBasePrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("MonthlyBasePrice").HasPrecision(18, 4).IsRequired();
            m.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(8).IsRequired();
        });
        builder.Navigation(x => x.MonthlyBasePrice).IsRequired();
        builder.Property(x => x.Interval).HasConversion<int>().HasDefaultValue(Contracts.PlanInterval.Monthly);
        builder.OwnsOne(x => x.AnnualPrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("AnnualPrice").HasPrecision(18, 4);
            m.Property(p => p.Currency).HasColumnName("AnnualPriceCurrency").HasMaxLength(8);
        });

        // Overage rates map to jsonb so the plan's pricing schedule is a single column.
        builder.Property<Dictionary<QuotaResource, decimal>>("_overageRates")
            .HasField("_overageRates")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v)
                    ? new Dictionary<QuotaResource, decimal>()
                    : JsonSerializer.Deserialize<Dictionary<QuotaResource, decimal>>(v, (JsonSerializerOptions?)null)
                        ?? new Dictionary<QuotaResource, decimal>())
            .HasColumnType("jsonb")
            .HasColumnName("OverageRates")
            .HasDefaultValueSql("'{}'::jsonb")
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<QuotaResource, decimal>>(
                (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
                v => v.Aggregate(0, (h, kv) => HashCode.Combine(h, (int)kv.Key, kv.Value.GetHashCode())),
                v => new Dictionary<QuotaResource, decimal>(v)));

        builder.Ignore(x => x.OverageRates);
        builder.Ignore(x => x.DomainEvents);
    }
}
