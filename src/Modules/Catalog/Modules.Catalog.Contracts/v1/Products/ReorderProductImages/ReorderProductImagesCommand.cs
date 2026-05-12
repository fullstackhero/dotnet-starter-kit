using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.ReorderProductImages;

/// <summary>
/// Reorder the product's images. Ids in <paramref name="OrderedImageIds"/> are set to
/// SortOrder = 0, 1, 2... in the order supplied; any images not listed are appended after.
/// </summary>
public sealed record ReorderProductImagesCommand(
    Guid ProductId,
    IReadOnlyList<Guid> OrderedImageIds) : ICommand<Unit>;
