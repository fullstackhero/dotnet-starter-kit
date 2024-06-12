using FSH.Framework.Core.Persistence;
using FSH.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Catalog.Application.Products.Update.v1;
public sealed class UpdateProductHandler(
    ILogger<UpdateProductHandler> logger,
    [FromKeyedServices("catalog:products")] IRepository<Product> repository)
    : IRequestHandler<UpdateProductRequest, UpdateProductResponse>
{
    public async Task<UpdateProductResponse> Handle(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var product = Product.Update(request.Id, request.Name!, request.Description, request.Price);
        await repository.UpdateAsync(product, cancellationToken);
        logger.LogInformation("product udpated {ProductId}", product.Id);
        return new UpdateProductResponse(product.Id, product.Name, product.Description, product.Price);
    }
}
