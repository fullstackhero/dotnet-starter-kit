using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public static class CreateDimensionEndpoint
{
    internal static RouteHandlerBuilder MapCreateDimensionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (CreateDimensionCommand request, ISender mediator) =>
                {
                    var response = await mediator.Send(request);
                    return Results.CreatedAtRoute(nameof(CreateDimensionEndpoint), new { id = response.Id }, response);
                })
                .WithName(nameof(CreateDimensionEndpoint))
                .WithSummary("Creates a dimension item")
                .WithDescription("Creates a dimension item")
                .Produces<CreateDimensionResponse>(StatusCodes.Status201Created)
                .RequirePermission("Permissions.Dimensions.Create")
                .MapToApiVersion(new ApiVersion(1, 0));

    }
}
