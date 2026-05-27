using System.Text.Json;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Multitenancy.Data.Configurations;

public class AppTenantInfoConfiguration : IEntityTypeConfiguration<AppTenantInfo>
{
    public void Configure(EntityTypeBuilder<AppTenantInfo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Tenants", MultitenancyConstants.Schema);

        builder.Property(t => t.Plan).HasMaxLength(64);

        builder.Property(t => t.QuotaLimits)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v)
                    ? new Dictionary<QuotaResource, long>()
                    : JsonSerializer.Deserialize<Dictionary<QuotaResource, long>>(v, (JsonSerializerOptions?)null)
                        ?? new Dictionary<QuotaResource, long>())
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<QuotaResource, long>>(
                (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
                v => v.Aggregate(0, (h, kv) => HashCode.Combine(h, (int)kv.Key, kv.Value.GetHashCode())),
                v => new Dictionary<QuotaResource, long>(v)));
    }
}