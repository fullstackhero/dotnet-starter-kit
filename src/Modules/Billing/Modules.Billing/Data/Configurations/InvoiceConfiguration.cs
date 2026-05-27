using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(8);
        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 4);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Notes).HasMaxLength(2048);

        builder.HasIndex(x => new { x.TenantId, x.PeriodYear, x.PeriodMonth })
            .IsUnique()
            .HasDatabaseName("ux_invoices_tenant_period");
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();

        builder.HasMany(x => x.LineItems)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Invoice.LineItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
