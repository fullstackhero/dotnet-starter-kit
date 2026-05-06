using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Categories;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.GetCategoryTree;

public sealed class GetCategoryTreeQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryTreeNodeDto>>
{
    public async ValueTask<IReadOnlyList<CategoryTreeNodeDto>> Handle(GetCategoryTreeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var all = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byParent = all
            .GroupBy(c => c.ParentCategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        IReadOnlyList<CategoryTreeNodeDto> Build(Guid? parentId)
        {
            if (!byParent.TryGetValue(parentId, out var children))
            {
                return Array.Empty<CategoryTreeNodeDto>();
            }
            return children
                .Select(c => new CategoryTreeNodeDto(c.Id, c.Name, c.Slug, c.Description, Build(c.Id)))
                .ToList();
        }

        return Build(null);
    }
}
