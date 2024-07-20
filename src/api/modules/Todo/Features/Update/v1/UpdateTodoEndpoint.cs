using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Todo.Features.Update.v1;
public static class UpdateTodoEndpoint
{
    internal static RouteHandlerBuilder MapTodoItemUpdationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.
            MapPut("/{id:guid}", async (Guid id, UpdateTodoCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateTodoEndpoint))
            .WithSummary("Updates a todo item")
            .WithDescription("Updated a todo item")
            .Produces<UpdateTodoResponse>(StatusCodes.Status200OK)
            .RequirePermission("Permissions.Todos.Update")
            .MapToApiVersion(new ApiVersion(1, 0));

    }
}
