using FSH.Framework.Core.Persistence;
using FSH.WebApi.Catalog.Domain;
using FSH.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Catalog.Application.Products.Delete.v1;
public sealed class DeleteProductHandler(
    ILogger<DeleteProductHandler> logger,
    [FromKeyedServices("catalog:products")] IRepository<Product> repository)
    : IRequestHandler<DeleteProductCommand, DeleteProductResponse>
{
    public async Task<DeleteProductResponse> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = product ?? throw new ProductNotFoundException(request.Id);
        await repository.DeleteAsync(product, cancellationToken);
        string deleteResponseMessage = $"product with id : {product.Id} deleted.";
        logger.LogInformation(deleteResponseMessage);
        return new DeleteProductResponse(deleteResponseMessage);
    }
}
