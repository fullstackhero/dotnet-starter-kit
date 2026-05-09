using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Bills.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class CreateBillEndpoint
{
    internal static RouteHandlerBuilder MapBillCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreateBillCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateBillEndpoint))
            .WithSummary("creates a bill")
            .WithDescription("creates a bill")
            .Produces<CreateBillResponse>()
            .RequirePermission("Permissions.Bills.Create")
            .MapToApiVersion(1);
    }
}
