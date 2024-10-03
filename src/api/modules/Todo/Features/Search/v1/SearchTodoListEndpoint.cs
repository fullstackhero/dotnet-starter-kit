using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Todo.Features.Search.v1;

public static class SearchTodoListEndpoint
{
    internal static RouteHandlerBuilder MapSearchTodoListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/search", async (ISender mediator, [FromBody] PaginationFilter filter) =>
        {
            var response = await mediator.Send(new SearchTodoListRequest(filter));
            return Results.Ok(response);
        })
        .WithName(nameof(SearchTodoListEndpoint))
        .WithSummary("Gets a list of todo items with paging support")
        .WithDescription("Gets a list of todo items with paging support")
        .Produces<PagedList<TodoDto>>()
        .RequirePermission("Permissions.Todos.View")
        .MapToApiVersion(1);
    }
}
