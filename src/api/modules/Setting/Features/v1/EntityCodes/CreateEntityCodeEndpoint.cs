using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public static class CreateEntityCodeEndpoint
{
    internal static RouteHandlerBuilder MapCreateEntityCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (CreateEntityCodeCommand request, ISender mediator) =>
                {
                    var response = await mediator.Send(request);
                    return Results.CreatedAtRoute(nameof(CreateEntityCodeEndpoint), new { id = response.Id }, response);
                })
                .WithName(nameof(CreateEntityCodeEndpoint))
                .WithSummary("Creates a EntityCode item")
                .WithDescription("Creates a EntityCode item")
                .Produces<CreateEntityCodeResponse>(StatusCodes.Status201Created)
                .RequirePermission("Permissions.EntityCodes.Create")
                .MapToApiVersion(new ApiVersion(1, 0));

    }
}
