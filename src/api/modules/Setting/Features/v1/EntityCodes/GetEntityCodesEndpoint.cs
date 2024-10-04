using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public static class GetEntityCodesEndpoint
{
    internal static RouteHandlerBuilder MapGetEntityCodesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/getlist", async (ISender mediator, [FromBody] GetEntityCodesRequest command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(GetEntityCodesEndpoint))
            .WithSummary("Gets a list of EntityCode")
            .WithDescription("Gets a list of EntityCode with filtering support")
            .Produces<List<EntityCodeDto>>()
            .RequirePermission("Permissions.EntityCodes.Search")
            .MapToApiVersion(1);
    }
}

