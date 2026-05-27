using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.AddProductImage;

/// <summary>
/// Attach an image to a product. <paramref name="Url"/> is the durable public URL (typically the
/// <c>publicUrl</c> returned by the Files module after a presigned upload). <paramref name="FileAssetId"/>
/// is the corresponding FileAsset id for bookkeeping; null when attaching an external URL.
/// </summary>
public sealed record AddProductImageCommand(
    Guid ProductId,
    Guid? FileAssetId,
    string Url) : ICommand<ProductImageDto>;
