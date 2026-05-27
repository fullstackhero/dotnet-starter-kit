using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.SearchProducts;

public sealed class SearchProductsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<SearchProductsQuery, PagedResponse<ProductDto>>
{
    public async ValueTask<PagedResponse<ProductDto>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var q = dbContext.Products.AsNoTracking().AsQueryable();

        if (query.BrandId is { } brandId)
        {
            q = q.Where(p => p.BrandId == brandId);
        }

        if (query.CategoryId is { } categoryId)
        {
            q = q.Where(p => p.CategoryId == categoryId);
        }

        if (query.IsActive is { } active)
        {
            q = q.Where(p => p.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(p =>
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                EF.Functions.ILike(p.Sku, $"%{term}%") ||
                EF.Functions.ILike(p.Slug, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var products = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<ProductDto>
        {
            Items = products.Select(p => p.ToDto()).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> q, string? sortBy, string? sortDir)
    {
        // Default to descending unless caller explicitly opts into ascending —
        // admins typically want newest-first when they don't pick a direction.
        bool desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "NAME" => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
            "SKU" => desc ? q.OrderByDescending(p => p.Sku) : q.OrderBy(p => p.Sku),
            "STOCK" => desc ? q.OrderByDescending(p => p.Stock) : q.OrderBy(p => p.Stock),
            "PRICE" => desc
                ? q.OrderByDescending(p => p.Price.Amount)
                : q.OrderBy(p => p.Price.Amount),
            _ => desc
                ? q.OrderByDescending(p => p.CreatedAtUtc)
                : q.OrderBy(p => p.CreatedAtUtc),
        };
    }
}
