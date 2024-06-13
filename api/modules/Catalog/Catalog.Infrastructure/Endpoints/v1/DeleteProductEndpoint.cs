using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.WebApi.Catalog.Application.Products.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteProductEndpoint
{
    internal static RouteHandlerBuilder MapProductDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/{id:guid}", (Guid id, ISender mediator) => mediator.Send(new DeleteProductCommand(id)))
                        .WithName(nameof(DeleteProductEndpoint))
                        .WithSummary("deletes product by id")
                        .WithDescription("deletes prodct by id")
                        .Produces<DeleteProductResponse>()
                        .RequirePermission("Permissions.Products.Delete")
                        .MapToApiVersion(1);
    }
}
