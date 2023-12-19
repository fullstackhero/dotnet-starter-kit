using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Application.Features.Products.ProductCreation.v1;
public static class ProductCreationEndpoint
{
    internal static RouteHandlerBuilder MapProductCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", (ProductCreationCommand request, ISender mediator) => mediator.Send(request))
            .WithName(nameof(ProductCreationEndpoint))
            .MapToApiVersion(1.0);
    }
}
