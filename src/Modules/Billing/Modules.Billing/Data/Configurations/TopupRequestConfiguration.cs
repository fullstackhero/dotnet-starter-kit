using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class TopupRequestConfiguration : IEntityTypeConfiguration<TopupRequest>
{
    public void Configure(EntityTypeBuilder<TopupRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TopupRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.OwnsOne(x => x.Amount, m =>
        {
            m.Property(p => p.Amount).HasColumnName("Amount").HasPrecision(18, 4).IsRequired();
            m.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(8).IsRequired();
        });
        builder.Navigation(x => x.Amount).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(512);
        builder.Property(x => x.DecisionNote).HasMaxLength(512);
        builder.Property(x => x.RequestedBy).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.InvoiceId);
        builder.Ignore(x => x.DomainEvents);
    }
}
