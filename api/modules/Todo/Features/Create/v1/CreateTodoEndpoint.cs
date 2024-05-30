using FSH.WebApi.Todo.Features.CreateTodo.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public static class CreateTodoEndpoint
{
    internal static RouteHandlerBuilder MapTodoItemCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (CreateTodoCommand request, ISender mediator) => mediator.Send(request))
                        .WithName(nameof(CreateTodoEndpoint))
                        .WithSummary("creates a todo item")
                        .WithDescription("creates a todo item")
                        .Produces<CreateTodoRepsonse>()
                        .MapToApiVersion(1);
    }
}
