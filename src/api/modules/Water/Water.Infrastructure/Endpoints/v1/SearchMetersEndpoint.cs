using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Meters.Get.v1;
using FSH.Starter.WebApi.Water.Application.Meters.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;

public static class SearchMetersEndpoint
{
    internal static RouteHandlerBuilder MapGetMeterListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchMetersCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchMetersEndpoint))
            .WithSummary("Gets a list of meters")
            .WithDescription("Gets a list of meters with pagination and filtering support")
            .Produces<PagedList<MeterResponse>>()
            .RequirePermission("Permissions.Meters.View")
            .MapToApiVersion(1);
    }
}
