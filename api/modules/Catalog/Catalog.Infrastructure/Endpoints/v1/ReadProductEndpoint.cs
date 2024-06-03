using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.WebApi.Catalog.Application.Products.Read.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Catalog.Infrastructure.Endpoints.v1
{
    public static class ReadProductEndpoint
    {
        internal static RouteHandlerBuilder MapProductReadEndpoint(this IEndpointRouteBuilder endpoints)
        {
            return endpoints
                .MapGet("/", (ReadProductCommand request, ISender mediator) => mediator.Send(request))
                .WithName(nameof(ReadProductEndpoint))
                .WithSummary("read a product")
                .WithDescription("read a product")
                .Produces<ReadProductResponse>()
                .RequirePermission("Permissions.Products.Read")
                .MapToApiVersion(1);
        }
    }
}

