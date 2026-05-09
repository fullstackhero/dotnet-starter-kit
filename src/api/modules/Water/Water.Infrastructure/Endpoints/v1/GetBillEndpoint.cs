using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Bills.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class GetBillEndpoint
{
    internal static RouteHandlerBuilder MapGetBillEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetBillRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetBillEndpoint))
            .WithSummary("gets bill by id")
            .WithDescription("gets bill by id")
            .Produces<BillResponse>()
            .RequirePermission("Permissions.Bills.View")
            .MapToApiVersion(1);
    }
}
