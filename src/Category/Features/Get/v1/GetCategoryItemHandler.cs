using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Domain;
using Category.Exceptions;
using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Category.Features.Get.v1;
 
public sealed class GetCategoryItemHandler(
    [FromKeyedServices("categoryItem")] IReadRepository<CategoryItem> repository,
    ICacheService cache)
    : IRequestHandler<GetCategoryItemRequest, GetCategoryItemResponse>
{
    public async Task<GetCategoryItemResponse> Handle(GetCategoryItemRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"categoryItem:{request.Id}",
            async () =>
            {
                var categoryItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (categoryItem == null) throw new CategoryItemNotFoundException(request.Id);
                return new GetCategoryItemResponse(categoryItem.Id, categoryItem.Name!, categoryItem.Description!);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
