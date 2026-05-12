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

        // The application sets Id via Guid.CreateVersion7() in ProductImage.Create. Without this,
        // EF's default ValueGeneratedOnAdd treats the non-default Guid as "already persisted"
        // when the entity is reached through a tracked parent's nav collection, so SaveChanges
        // emits an UPDATE that affects 0 rows → DbUpdateConcurrencyException.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ProductId).IsRequired();
        builder.HasIndex(x => x.ProductId);

        builder.Property(x => x.FileAssetId);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.IsThumbnail).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Non-unique index for cheap product-scoped lookups (e.g. cascade delete, queries that
        // pull all images for a product).
        //
        // We intentionally do NOT add a partial UNIQUE INDEX on (ProductId) WHERE IsThumbnail=TRUE
        // to enforce "single thumbnail per product". Such an index check is per-statement in
        // Postgres (partial unique indexes can't be DEFERRABLE), and Product.SetThumbnail emits
        // two UPDATEs in a single SaveChanges — demote-old + promote-new — whose order EF can
        // pick freely. If EF emits promote-first, the intermediate state has two thumbnails and
        // the constraint fires. The single-thumbnail invariant is enforced by the aggregate
        // itself: AddImage promotes only when no images exist, SetThumbnail clears the flag on
        // every other image via a foreach, RemoveImage auto-promotes the next lowest-sorted
        // image when the cover is removed.
        builder.HasIndex(x => x.ProductId);
    }
}
