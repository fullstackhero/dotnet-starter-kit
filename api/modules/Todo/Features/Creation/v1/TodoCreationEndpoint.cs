using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public static class TodoCreationEndpoint
{
    internal static RouteHandlerBuilder MapTodoItemCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (TodoCreationCommand request, ISender mediator) => mediator.Send(request))
                        .WithName(nameof(TodoCreationEndpoint))
                        .MapToApiVersion(1.0);
    }
}
