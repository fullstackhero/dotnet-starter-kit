using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Products.GetList.v1;

public sealed class GetProductsSpecs : EntitiesByBaseFilterSpec<Product, ProductDto>
{
    public GetProductsSpecs(GetProductsRequest command)
        : base(command) =>
        Query
            .OrderBy(c => c.Name)
            .Where(p => p.Price >= command.MinimumRate!.Value, command.MinimumRate.HasValue)
            .Where(p => p.Price <= command.MaximumRate!.Value, command.MaximumRate.HasValue);
}
