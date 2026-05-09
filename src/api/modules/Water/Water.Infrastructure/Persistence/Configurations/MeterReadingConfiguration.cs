using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Water.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Water.Infrastructure.Persistence.Configurations;

internal sealed class MeterReadingConfiguration : IEntityTypeConfiguration<MeterReading>
{
    public void Configure(EntityTypeBuilder<MeterReading> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReadingValue).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.PreviousReadingValue).HasPrecision(18, 6);
        builder.Property(x => x.Consumption).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.Meter)
            .WithMany()
            .HasForeignKey(x => x.MeterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
