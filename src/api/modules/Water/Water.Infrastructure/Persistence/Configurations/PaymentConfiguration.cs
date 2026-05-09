using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Water.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Water.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountPaid).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.TransactionReference).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasOne(x => x.Bill)
            .WithMany()
            .HasForeignKey(x => x.BillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
