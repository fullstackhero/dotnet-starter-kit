using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Todo.Features.Create.v1;
public static class CreateTodoEndpoint
{
    internal static RouteHandlerBuilder MapTodoItemCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (CreateTodoCommand request, ISender mediator) => mediator.Send(request))
                        .WithName(nameof(CreateTodoEndpoint))
                        .WithSummary("creates a todo item")
                        .WithDescription("creates a todo item")
                        .Produces<CreateTodoRepsonse>()
                        .RequirePermission("Permissions.Todos.Create")
                        .MapToApiVersion(1);
    }
}
