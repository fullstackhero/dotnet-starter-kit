using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Todo.Features.GetList.v1;
public static class GetTodoListEndpoint
{
    internal static RouteHandlerBuilder MapGetTodoListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/search", (ISender mediator, PaginationFilter filter) =>
        {
            var response = mediator.Send(new GetTodoListRequest(filter));
            return Results.Ok(response);
        })
            .WithName(nameof(GetTodoListEndpoint))
            .WithSummary("gets a list of todo items with paging support")
            .WithDescription("gets a list of todo items with paging support")
            .Produces<PagedList<TodoDto>>()
            .RequirePermission("Permissions.Todos.View")
            .MapToApiVersion(1);
    }
}
