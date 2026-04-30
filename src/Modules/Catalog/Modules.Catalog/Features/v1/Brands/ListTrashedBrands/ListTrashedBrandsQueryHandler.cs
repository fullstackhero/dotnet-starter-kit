using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands;

public sealed class ListTrashedBrandsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<ListTrashedBrandsQuery, PagedResponse<BrandDto>>
{
    public async ValueTask<PagedResponse<BrandDto>> Handle(
        ListTrashedBrandsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        // IgnoreQueryFilters([SoftDelete]) bypasses ONLY the soft-delete filter;
        // tenant scoping (Finbuckle) stays in force, so a tenant only sees its
        // own trashed rows. Most-recently-deleted first.
        var q = dbContext.Brands
            .AsNoTracking()
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(b => b.IsDeleted)
            .OrderByDescending(b => b.DeletedOnUtc);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => new BrandDto(
                b.Id, b.Name, b.Slug, b.Description, b.LogoUrl,
                b.CreatedAtUtc, b.UpdatedAtUtc, b.DeletedOnUtc, b.DeletedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<BrandDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }
}
