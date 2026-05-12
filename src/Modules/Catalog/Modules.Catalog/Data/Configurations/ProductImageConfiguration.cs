using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId).IsRequired();
        builder.HasIndex(x => x.ProductId);

        builder.Property(x => x.FileAssetId);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.IsThumbnail).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // At most one thumbnail per product. Partial unique index — Postgres-only HasFilter
        // syntax matches the project convention used elsewhere (e.g. ProductConfiguration's
        // Sku/Slug unique-on-live-rows filter).
        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("\"IsThumbnail\" = TRUE")
            .HasDatabaseName("IX_ProductImages_ProductId_Thumbnail");
    }
}
