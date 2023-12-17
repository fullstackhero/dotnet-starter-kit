using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Modules.Catalog.Products.Features.ProductCreation.v1;
public static class ProductCreationEndpoint
{
    internal static RouteHandlerBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/", (ProductCreationCommand request, ISender mediator) => mediator.Send(request)
        )
        .WithName(nameof(ProductCreationEndpoint))
        .WithTags("products")
        .MapToApiVersion(1.0);
    }
}
