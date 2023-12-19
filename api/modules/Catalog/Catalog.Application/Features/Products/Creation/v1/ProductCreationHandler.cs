using FSH.WebApi.Catalog.Domain;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Catalog.Application.Features.Products.Creation.v1;
public sealed class ProductCreationHandler(ILogger<ProductCreationHandler> logger) : IRequestHandler<ProductCreationCommand, Guid>
{
    public async Task<Guid> Handle(ProductCreationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await Task.FromResult(0).ConfigureAwait(false);
        var product = request.Adapt<Product>();
        logger.LogInformation("product created {ProductId}", product.Id);
        return product.Id;
    }
}
