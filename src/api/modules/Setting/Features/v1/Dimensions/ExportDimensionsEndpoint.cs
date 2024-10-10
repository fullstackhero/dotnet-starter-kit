using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public static class ExportDimensionsEndpoint
{
    internal static RouteHandlerBuilder MapExportDimensionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/export", async Task<byte[]> (ISender mediator, [FromBody] ExportDimensionsRequest command) =>
            {
                var response = await mediator.Send(command);

                return response;
            })
            .WithName(nameof(ExportDimensionsEndpoint))
            .WithSummary("Exports a list of Dimensions")
            .WithDescription("Exports a list of Dimensions with filtering support")
            .Produces <byte[]>()
            .RequirePermission("Permissions.Dimensions.Export")
            .MapToApiVersion(1);
    }
}

