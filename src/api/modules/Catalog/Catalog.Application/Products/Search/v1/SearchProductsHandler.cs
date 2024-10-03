using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;


namespace FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
public sealed class SearchProductsHandler(
    [FromKeyedServices("catalog:products")] IReadRepository<Product> repository)
    : IRequestHandler<SearchProductsRequest, PagedList<ProductDto>>
{
    public async Task<PagedList<ProductDto>> Handle(SearchProductsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchProductsSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<ProductDto>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}

