using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;
using FSH.Starter.WebApi.Water.Application.MeterReadings.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;

public static class SearchMeterReadingsEndpoint
{
    internal static RouteHandlerBuilder MapGetMeterReadingListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchMeterReadingsCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchMeterReadingsEndpoint))
            .WithSummary("Gets a list of meter readings")
            .WithDescription("Gets a list of meter readings with pagination and filtering support")
            .Produces<PagedList<MeterReadingResponse>>()
            .RequirePermission("Permissions.MeterReadings.View")
            .MapToApiVersion(1);
    }
}
