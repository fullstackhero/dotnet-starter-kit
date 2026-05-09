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

        var byParent = all.ToLookup(c => c.ParentCategoryId);

        IReadOnlyList<CategoryTreeNodeDto> Build(Guid? parentId)
        {
            return byParent[parentId]
                .Select(c => new CategoryTreeNodeDto(c.Id, c.Name, c.Slug, c.Description, Build(c.Id)))
                .ToList();
        }

        return Build(null);
    }
}
