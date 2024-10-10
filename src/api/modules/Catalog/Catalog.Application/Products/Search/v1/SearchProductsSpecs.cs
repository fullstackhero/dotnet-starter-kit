using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
public sealed class SearchProductsSpecs : EntitiesByPaginationFilterSpec<Product, ProductDto>
{
    public SearchProductsSpecs(SearchProductsRequest request)
        : base(request) =>
        Query
            .OrderBy(c => c.Name, !request.HasOrderBy())
            .Where(p => p.Price >= request.MinimumRate!.Value, request.MinimumRate.HasValue)
            .Where(p => p.Price <= request.MaximumRate!.Value, request.MaximumRate.HasValue);
}
