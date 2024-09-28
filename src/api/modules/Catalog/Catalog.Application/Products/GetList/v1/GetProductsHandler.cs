using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Catalog.Application.Products.GetList.v1;

public class GetProductsHandler(
    [FromKeyedServices("catalog:products")]  IReadRepository<Product> repository)
    : IRequestHandler<GetProductsCommand, List<ProductResponse>>
{
    public async Task<List<ProductResponse>> Handle(GetProductsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var spec = new EntitiesByBaseFilterSpec<Product, ProductResponse>(request.Filter);
        
        return await repository.ListAsync(spec, cancellationToken);
    }
}
