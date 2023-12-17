using FSH.WebApi.Modules.Catalog.Products.Models;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Modules.Catalog.Products.Features.ProductCreation.v1;
public sealed class ProductCreationHandler : IRequestHandler<ProductCreationCommand, IResult>
{
    private readonly ILogger<ProductCreationHandler> _logger;

    public ProductCreationHandler(ILogger<ProductCreationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<IResult> Handle(ProductCreationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await Task.FromResult(0).ConfigureAwait(false);
        var product = request.Adapt<Product>();
        _logger.LogInformation("product created {ProductId}", product.Id);
        return Results.Created(nameof(ProductCreationEndpoint), new { id = product.Id });
    }
}
