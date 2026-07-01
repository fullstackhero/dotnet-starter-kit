using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("InvoiceLineItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.Resource).HasConversion<int?>();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(512);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.OwnsOne(x => x.Amount, m =>
        {
            m.Property(p => p.Amount).HasColumnName("Amount").HasPrecision(18, 4).IsRequired();
            m.Property(p => p.Currency).HasColumnName("AmountCurrency").HasMaxLength(8).IsRequired();
        });
        builder.Navigation(x => x.Amount).IsRequired();

        builder.HasIndex(x => x.InvoiceId);

        builder.Ignore(x => x.DomainEvents);
    }
}
