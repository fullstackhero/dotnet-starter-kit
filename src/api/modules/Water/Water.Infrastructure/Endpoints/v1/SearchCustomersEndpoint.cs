using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Customers.Get.v1;
using FSH.Starter.WebApi.Water.Application.Customers.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;

public static class SearchCustomersEndpoint
{
    internal static RouteHandlerBuilder MapGetCustomerListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchCustomersCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchCustomersEndpoint))
            .WithSummary("Gets a list of customers")
            .WithDescription("Gets a list of customers with pagination and filtering support")
            .Produces<PagedList<CustomerResponse>>()
            .RequirePermission("Permissions.Customers.View")
            .MapToApiVersion(1);
    }
}
