using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Todo.Features.Create.v1;
public static class CreateTodoEndpoint
{
    internal static RouteHandlerBuilder MapTodoItemCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (CreateTodoCommand request, ISender mediator) =>
                {
                    var response = await mediator.Send(request);
                    return Results.CreatedAtRoute(nameof(CreateTodoEndpoint), new { id = response.Id }, response);
                })
                .WithName(nameof(CreateTodoEndpoint))
                .WithSummary("Creates a todo item")
                .WithDescription("Creates a todo item")
                .Produces<CreateTodoResponse>(StatusCodes.Status201Created)
                .RequirePermission("Permissions.Todos.Create")
                .MapToApiVersion(new ApiVersion(1, 0));

    }
}
