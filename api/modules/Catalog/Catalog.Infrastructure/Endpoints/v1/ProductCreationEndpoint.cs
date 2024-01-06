using FSH.WebApi.Catalog.Application.Products.Creation.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class ProductCreationEndpoint
{
    internal static RouteHandlerBuilder MapProductCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", (ProductCreationCommand request, ISender mediator) => mediator.Send(request))
            .WithName(nameof(ProductCreationEndpoint))
            .WithSummary("creates a product")
            .WithDescription("creates a product")
            .Produces<ProductCreationResponse>()
            .MapToApiVersion(1);
    }
}
