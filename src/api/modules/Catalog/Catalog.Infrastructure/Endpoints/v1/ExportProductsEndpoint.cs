using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;

public static class ExportProductsEndpoint
{
    internal static RouteHandlerBuilder MapExportProductsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/export", async Task<byte[]> (ISender mediator, [FromBody] ExportProductsRequest command) =>
            {
                var response = await mediator.Send(command);

                return response;
            })
            .WithName(nameof(ExportProductsEndpoint))
            .WithSummary("Exports a list of products")
            .WithDescription("Exports a list of products with filtering support")
            .Produces <byte[]>()
            .RequirePermission("Permissions.Products.Export")
            .MapToApiVersion(1);
    }
}

