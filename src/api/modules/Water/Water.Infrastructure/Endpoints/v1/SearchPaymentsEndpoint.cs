using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Payments.Get.v1;
using FSH.Starter.WebApi.Water.Application.Payments.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;

public static class SearchPaymentsEndpoint
{
    internal static RouteHandlerBuilder MapGetPaymentListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchPaymentsCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchPaymentsEndpoint))
            .WithSummary("Gets a list of payments")
            .WithDescription("Gets a list of payments with pagination and filtering support")
            .Produces<PagedList<PaymentResponse>>()
            .RequirePermission("Permissions.Payments.View")
            .MapToApiVersion(1);
    }
}
