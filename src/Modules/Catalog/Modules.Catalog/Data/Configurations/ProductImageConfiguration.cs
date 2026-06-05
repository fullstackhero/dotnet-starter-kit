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

        // Id is app-assigned (Guid.CreateVersion7). Without ValueGeneratedNever, EF treats the non-default
        // Guid reached via a tracked parent's nav collection as persisted → UPDATE-0-rows → concurrency ex.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ProductId).IsRequired();
        builder.HasIndex(x => x.ProductId);

        builder.Property(x => x.FileAssetId);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.IsThumbnail).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Non-unique index for cheap product-scoped lookups. No partial UNIQUE on (ProductId) WHERE IsThumbnail:
        // it can't be DEFERRABLE and fires mid-statement on SetThumbnail; the aggregate enforces single-thumbnail.
        builder.HasIndex(x => x.ProductId);
    }
}
