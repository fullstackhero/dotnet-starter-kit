using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Water.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Water.Infrastructure.Persistence.Configurations;

internal sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.RatePerUnit).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.FixedCharge).HasPrecision(18, 6).IsRequired();
    }
}
