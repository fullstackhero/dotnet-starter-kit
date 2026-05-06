using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sku).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.Sku).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.ImageUrl).HasMaxLength(512);

        builder.Property(x => x.BrandId).IsRequired();
        builder.HasIndex(x => x.BrandId);

        builder.Property(x => x.CategoryId).IsRequired();
        builder.HasIndex(x => x.CategoryId);

        builder.Property(x => x.Stock).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.OwnsOne(x => x.Price, m =>
        {
            m.Property(p => p.Amount).HasColumnName("PriceAmount").HasPrecision(18, 4).IsRequired();
            m.Property(p => p.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).IsRequired();
        });

        builder.Navigation(x => x.Price).IsRequired();

        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
