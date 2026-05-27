using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Domain.Events;

namespace FSH.Modules.Catalog.Domain;

public sealed class Product : AggregateRoot<Guid>, ISoftDeletable
{
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid BrandId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Money Price { get; private set; } = default!;
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    // EF populates this via the navigation property; aggregate methods mutate through the
    // private list so invariants (single thumbnail, contiguous SortOrder) hold.
    private readonly List<ProductImage> _images = [];
    public IReadOnlyList<ProductImage> Images => _images;

    /// <summary>The thumbnail (cover) image URL, or null when the product has no images.</summary>
    public string? ThumbnailUrl => _images.FirstOrDefault(i => i.IsThumbnail)?.Url;

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private Product() { }

    public static Product Create(
        string sku,
        string name,
        string? description,
        Guid brandId,
        Guid categoryId,
        Money price,
        int stock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(price);
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock cannot be negative.");
        }
        if (brandId == Guid.Empty)
        {
            throw new ArgumentException("BrandId is required.", nameof(brandId));
        }
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        }

        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Slug = Slugify(name),
            Description = description?.Trim(),
            BrandId = brandId,
            CategoryId = categoryId,
            Price = price,
            Stock = stock,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        product.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ProductCreatedDomainEvent(product.Id, product.Sku, product.Name, id, ts)));

        return product;
    }

    public void Update(
        string name,
        string? description,
        Guid brandId,
        Guid categoryId,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (brandId == Guid.Empty)
        {
            throw new ArgumentException("BrandId is required.", nameof(brandId));
        }
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        }

        Name = name.Trim();
        Slug = Slugify(name);
        Description = description?.Trim();
        BrandId = brandId;
        CategoryId = categoryId;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        if (newPrice == Price)
        {
            return;
        }

        decimal oldAmount = Price.Amount;
        Price = newPrice;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ProductPriceChangedDomainEvent(Id, oldAmount, newPrice.Amount, newPrice.Currency, id, ts)));
    }

    public void AdjustStock(int delta)
    {
        int newStock = Stock + delta;
        if (newStock < 0)
        {
            throw new InvalidOperationException(
                $"Stock adjustment of {delta} would result in negative stock (current: {Stock}).");
        }

        int oldStock = Stock;
        Stock = newStock;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ProductStockAdjustedDomainEvent(Id, oldStock, newStock, delta, id, ts)));
    }

    // ─── Image management ─────────────────────────────────────────────────

    /// <summary>
    /// Attach a new image. The first image attached is automatically the thumbnail; subsequent
    /// images come in non-thumbnail and the caller can promote one via <see cref="SetThumbnail"/>.
    /// </summary>
    public ProductImage AddImage(Guid? fileAssetId, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        bool isFirst = _images.Count == 0;
        int order = isFirst ? 0 : _images.Max(i => i.SortOrder) + 1;
        var image = ProductImage.Create(Id, fileAssetId, url, isThumbnail: isFirst, sortOrder: order);
        _images.Add(image);
        UpdatedAtUtc = DateTime.UtcNow;
        return image;
    }

    /// <summary>
    /// Remove an image. If the removed image was the thumbnail and other images remain, the
    /// lowest-sorted remaining image is promoted to thumbnail so the product always has a cover.
    /// </summary>
    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new InvalidOperationException($"Image {imageId} not found on product {Id}.");
        bool wasThumbnail = image.IsThumbnail;
        _images.Remove(image);

        if (wasThumbnail && _images.Count > 0)
        {
            var promoted = _images.OrderBy(i => i.SortOrder).First();
            promoted.MarkThumbnail(true);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Mark <paramref name="imageId"/> as the thumbnail; clears the flag on every other image.</summary>
    public void SetThumbnail(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new InvalidOperationException($"Image {imageId} not found on product {Id}.");
        if (image.IsThumbnail) return;

        foreach (var i in _images)
        {
            i.MarkThumbnail(i.Id == imageId);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Reorder images by the supplied id sequence. Ids not in <paramref name="orderedImageIds"/> are appended in their existing order after the rest.</summary>
    public void ReorderImages(IReadOnlyList<Guid> orderedImageIds)
    {
        ArgumentNullException.ThrowIfNull(orderedImageIds);
        int order = 0;
        var seen = new HashSet<Guid>();
        foreach (var id in orderedImageIds)
        {
            var image = _images.FirstOrDefault(i => i.Id == id);
            if (image is null) continue;
            image.SetSortOrder(order++);
            seen.Add(id);
        }
        foreach (var trailing in _images.Where(i => !seen.Contains(i.Id)).OrderBy(i => i.SortOrder).ToList())
        {
            trailing.SetSortOrder(order++);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Slugify(string value)
    {
        var trimmed = value.Trim();
#pragma warning disable CA1308 // slug is canonical lowercase, not security-sensitive
        var lower = trimmed.ToLowerInvariant();
#pragma warning restore CA1308
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars).Trim('-');
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }
        return collapsed;
    }
}
