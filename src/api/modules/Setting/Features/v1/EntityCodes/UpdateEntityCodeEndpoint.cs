using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public static class UpdateEntityCodeEndpoint
{
    internal static RouteHandlerBuilder MapUpdateEntityCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.
            MapPut("/{id:guid}", async (Guid id, UpdateEntityCodeCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateEntityCodeEndpoint))
            .WithSummary("Updates a EntityCode item")
            .WithDescription("Updated a EntityCode item")
            .Produces<UpdateEntityCodeResponse>(StatusCodes.Status200OK)
            .RequirePermission("Permissions.EntityCodes.Update")
            .MapToApiVersion(new ApiVersion(1, 0));

    }
}
