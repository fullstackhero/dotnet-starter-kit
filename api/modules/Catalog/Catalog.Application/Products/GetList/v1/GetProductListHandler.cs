using FSH.WebApi.Catalog.Application.Products.Get.v1;
using Microsoft.Extensions.DependencyInjection;
using FSH.Framework.Core.Specifications;
using FSH.Framework.Core.Persistence;
using FSH.WebApi.Catalog.Domain;
using FSH.Framework.Core.Paging;
using MediatR;


namespace FSH.WebApi.Catalog.Application.Products.GetList.v1;
public sealed class GetProductListHandler(
    [FromKeyedServices("catalog:products")] IReadRepository<Product> repository)
    : IRequestHandler<GetProductListRequest, PagedList<GetProductResponse>>
{
    public async Task<PagedList<GetProductResponse>> Handle(GetProductListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByPaginationFilterSpec<Product, GetProductResponse>(request.filter);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<GetProductResponse>(items, request.filter.PageNumber, request.filter.PageSize, totalCount);
    }
}

