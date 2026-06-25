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
        builder.Property(x => x.Purpose).HasConversion<int>().HasDefaultValue(Contracts.InvoicePurpose.Usage);
        builder.Property(x => x.PeriodStartUtc);
        builder.Property(x => x.PeriodEndUtc);
        builder.Property(x => x.Notes).HasMaxLength(2048);

        // One invoice per tenant per month *per purpose* — subscription (term base fee) and usage
        // (metered overage) are separate streams that may both fall in the same calendar month.
        // Recurring invoices are unique per tenant/period/purpose; Topup invoices (Purpose=2) are
        // ad-hoc and may repeat within a period, so exclude them from the uniqueness filter.
        builder.HasIndex(x => new { x.TenantId, x.PeriodYear, x.PeriodMonth, x.Purpose })
            .IsUnique()
            .HasFilter($"\"Purpose\" <> {(int)Contracts.InvoicePurpose.Topup}")
            .HasDatabaseName("ux_invoices_tenant_period_purpose");
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
