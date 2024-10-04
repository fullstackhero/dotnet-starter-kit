using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public static class UpdateDimensionEndpoint
{
    internal static RouteHandlerBuilder MapUpdateDimensionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.
            MapPut("/{id:guid}", async (Guid id, UpdateDimensionCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateDimensionEndpoint))
            .WithSummary("Updates a dimension item")
            .WithDescription("Updated a dimension item")
            .Produces<UpdateDimensionResponse>(StatusCodes.Status200OK)
            .RequirePermission("Permissions.Dimensions.Update")
            .MapToApiVersion(new ApiVersion(1, 0));

    }
}
