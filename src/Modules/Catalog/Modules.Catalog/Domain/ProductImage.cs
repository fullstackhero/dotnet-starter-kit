using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// A product image. Owned by <see cref="Product"/> (cascade-deleted with the parent).
/// <para>
/// <b>Url</b> is the durable, persisted public URL — captured at attach time from the Files
/// module's <c>BuildPublicUrl</c> output so the product page doesn't need to re-fetch a
/// FileAsset on every render.
/// </para>
/// <para>
/// <b>FileAssetId</b> is nullable: images attached via the Files presigned-upload flow carry
/// the FileAsset id for bookkeeping (delete = best-effort cleanup of the storage object), but
/// images pasted as external URLs (legacy / non-managed) have no FileAsset.
/// </para>
/// <para>
/// <b>IsThumbnail</b>: exactly one image per product carries <c>true</c>. The product's
/// "cover" image. <see cref="Product.SetThumbnail"/> enforces uniqueness within the aggregate.
/// </para>
/// </summary>
public sealed class ProductImage : BaseEntity<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid? FileAssetId { get; private set; }
    public string Url { get; private set; } = default!;
    public bool IsThumbnail { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ProductImage() { }

    internal static ProductImage Create(
        Guid productId,
        Guid? fileAssetId,
        string url,
        bool isThumbnail,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new ProductImage
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            FileAssetId = fileAssetId,
            Url = url.Trim(),
            IsThumbnail = isThumbnail,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    internal void MarkThumbnail(bool value) => IsThumbnail = value;

    internal void SetSortOrder(int order) => SortOrder = order;
}
