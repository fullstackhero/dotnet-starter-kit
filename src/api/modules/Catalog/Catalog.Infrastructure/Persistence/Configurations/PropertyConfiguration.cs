using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Persistence.Configurations;
internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.AskingPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SoldPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.FeatureList).HasMaxLength(2000);

        builder.HasOne(x => x.Neighborhood)
            .WithMany()
            .HasForeignKey(x => x.NeighborhoodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PropertyType)
            .WithMany()
            .HasForeignKey(x => x.PropertyTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}