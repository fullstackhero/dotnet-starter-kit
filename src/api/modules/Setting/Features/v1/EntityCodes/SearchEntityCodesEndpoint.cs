using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public static class SearchEntityCodesEndpoint
{
    internal static RouteHandlerBuilder MapSearchEntityCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/search", async (ISender mediator, [FromBody] SearchEntityCodesRequest command) =>
        {
            var response = await mediator.Send(command);
            return Results.Ok(response);
        })
        .WithName(nameof(SearchEntityCodesEndpoint))
        .WithSummary("Gets a list of EntityCode items with paging support")
        .WithDescription("Gets a list of EntityCode items with paging support")
        .Produces<PagedList<EntityCodeDto>>()
        .RequirePermission("Permissions.EntityCodes.View")
        .MapToApiVersion(1);
    }
}
