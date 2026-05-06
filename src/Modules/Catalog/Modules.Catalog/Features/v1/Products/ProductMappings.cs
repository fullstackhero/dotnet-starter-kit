using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Domain;

namespace FSH.Modules.Catalog.Features.v1.Products;

internal static class ProductMappings
{
    public static ProductDto ToDto(this Product p) => new(
        p.Id,
        p.Sku,
        p.Name,
        p.Slug,
        p.Description,
        p.BrandId,
        p.CategoryId,
        new MoneyDto(p.Price.Amount, p.Price.Currency),
        p.Stock,
        p.IsActive,
        p.ImageUrl,
        p.CreatedAtUtc,
        p.UpdatedAtUtc,
        p.DeletedOnUtc,
        p.DeletedBy);
}
