using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Todo.Features.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Todo.Features.GetList.v1;

public static class GetTodoListEndpoint
{
    internal static RouteHandlerBuilder MapGetTodoListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/getlist", async (ISender mediator, [FromBody] BaseFilter filter) =>
            {
                var response = await mediator.Send(new GetTodoListRequest(filter));
                return Results.Ok(response);
            })
            .WithName(nameof(GetTodoListEndpoint))
            .WithSummary("Gets a list of todo")
            .WithDescription("Gets a list of todo with filtering support")
            .Produces<List<TodoDto>>()
            .RequirePermission("Permissions.Todos.Search")
            .MapToApiVersion(1);
    }
}

