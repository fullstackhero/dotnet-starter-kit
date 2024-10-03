using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Catalog.Application.Products.GetList.v1;

public class GetProductsHandler(
    [FromKeyedServices("catalog:products")]  IReadRepository<Product> repository)
    : IRequestHandler<GetProductsRequest, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetProductsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var spec = new EntitiesByBaseFilterSpec<Product, ProductDto>(request.Filter);
        
        return await repository.ListAsync(spec, cancellationToken);
    }
}
