using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Water.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Water.Infrastructure.Persistence.Configurations;

internal sealed class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalConsumption).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.FixedCharge).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.VariableCharge).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Tariff)
            .WithMany()
            .HasForeignKey(x => x.TariffId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
