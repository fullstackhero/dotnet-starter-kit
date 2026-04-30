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
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

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
        int stock,
        string? imageUrl)
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
            ImageUrl = imageUrl?.Trim(),
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
        string? imageUrl,
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
        ImageUrl = imageUrl?.Trim();
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
