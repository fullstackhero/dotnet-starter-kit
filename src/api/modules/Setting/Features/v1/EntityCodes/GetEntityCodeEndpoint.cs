using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public static class GetEntityCodeEndpoint
{
    internal static RouteHandlerBuilder MapGetEntityCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
                        {
                            var response = await mediator.Send(new GetEntityCodeRequest(id));
                            return Results.Ok(response);
                        })
                        .WithName(nameof(GetEntityCodeEndpoint))
                        .WithSummary("gets EntityCode item by id")
                        .WithDescription("gets EntityCode item by id")
                        .Produces<GetEntityCodeResponse>()
                        .RequirePermission("Permissions.EntityCodes.View")
                        .MapToApiVersion(1);
    }
}
