using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Bills.Get.v1;
using FSH.Starter.WebApi.Water.Application.Bills.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;

public static class SearchBillsEndpoint
{
    internal static RouteHandlerBuilder MapGetBillListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchBillsCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchBillsEndpoint))
            .WithSummary("Gets a list of bills")
            .WithDescription("Gets a list of bills with pagination and filtering support")
            .Produces<PagedList<BillResponse>>()
            .RequirePermission("Permissions.Bills.View")
            .MapToApiVersion(1);
    }
}
