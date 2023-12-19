using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public static class TodoItemCreationEndpoint
{
    internal static RouteHandlerBuilder MapTodoItemCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (TodoItemCreationCommand request, ISender mediator) => mediator.Send(request))
                        .WithName(nameof(TodoItemCreationEndpoint))
                        .MapToApiVersion(1.0);
    }
}
