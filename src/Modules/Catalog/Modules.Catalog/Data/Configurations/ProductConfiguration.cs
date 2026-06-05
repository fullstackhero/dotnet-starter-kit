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
        // Tenant isolation auto-applied by BaseDbContext; the shadow TenantId column makes Sku/Slug
        // unique-per-tenant, so two tenants can share "ABC-001". Opt out via IGlobalEntity.

        builder.Property(x => x.Sku).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.Sku).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Description).HasMaxLength(4000);

        // Child collection: ProductImage rows cascade-delete with the product. AutoInclude
        // because product reads typically need the cover image and the join is small.
        builder.HasMany(x => x.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Images).AutoInclude();

        // Derived from the Images collection — not a column.
        builder.Ignore(x => x.ThumbnailUrl);

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
