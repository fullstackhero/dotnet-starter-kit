using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.WebApi.Catalog.Application.Products.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdateProductEndpoint
{
    internal static RouteHandlerBuilder MapProductUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/{id:guid}", (Guid id, UpdateProductCommand request, ISender mediator) => mediator.Send(new UpdateProductRequest(id, request.Name, request.Description, request.Price)))
            .WithName(nameof(UpdateProductEndpoint))
            .WithSummary("update a product")
            .WithDescription("update a product")
            .Produces<UpdateProductResponse>()
            .RequirePermission("Permissions.Products.Update")
            .MapToApiVersion(1);
    }
}
