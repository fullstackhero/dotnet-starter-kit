using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public static class ExportEntityCodesEndpoint
{
    internal static RouteHandlerBuilder MapExportEntityCodesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/export", async Task<byte[]> (ISender mediator, [FromBody] ExportEntityCodesRequest command) =>
            {
                var response = await mediator.Send(command);

                return response;
            })
            .WithName(nameof(ExportEntityCodesEndpoint))
            .WithSummary("Exports a list of EntityCodes")
            .WithDescription("Exports a list of EntityCodes with filtering support")
            .Produces <byte[]>()
            .RequirePermission("Permissions.EntityCodes.Export")
            .MapToApiVersion(1);
    }
}

