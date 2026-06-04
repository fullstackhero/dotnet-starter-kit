using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Multitenancy.Data.Configurations;

public sealed class TenantExpiryNoticeConfiguration : IEntityTypeConfiguration<TenantExpiryNotice>
{
    public void Configure(EntityTypeBuilder<TenantExpiryNotice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TenantExpiryNotices", MultitenancyConstants.Schema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NoticeType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ValidUptoUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // One notice per tenant per state per validity period — the dedup guarantee.
        builder.HasIndex(x => new { x.TenantId, x.NoticeType, x.ValidUptoUtc })
            .IsUnique()
            .HasDatabaseName("ux_tenant_expiry_notices_tenant_type_validupto");
    }
}
