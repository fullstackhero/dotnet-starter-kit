using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Domain;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Category.Features.GetList.v1;
 
public sealed class GetCategoryItemListHandler(
    [FromKeyedServices("categoryItem")] IReadRepository<CategoryItem> repository)
    : IRequestHandler<GetCategoryItemListRequest, PagedList<CategoryItemDto>>
{
    public async Task<PagedList<CategoryItemDto>> Handle(GetCategoryItemListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByPaginationFilterSpec<CategoryItem, CategoryItemDto>(request.Filter);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<CategoryItemDto>(items, request.Filter.PageNumber, request.Filter.PageSize, totalCount);
    }
}
