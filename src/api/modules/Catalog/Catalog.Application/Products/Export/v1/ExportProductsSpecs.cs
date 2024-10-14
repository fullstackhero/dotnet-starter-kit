using Ardalis.Specification;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;

public sealed class ExportProductsSpecs : EntitiesByBaseFilterSpec<Product, ProductDto>
{
    public ExportProductsSpecs(ExportProductsRequest request)
        : base(request) =>
        Query
            .OrderBy(c => c.Name)
            .Where(p => p.Price >= request.MinimumRate!.Value, request.MinimumRate.HasValue)
            .Where(p => p.Price <= request.MaximumRate!.Value, request.MaximumRate.HasValue);
}
