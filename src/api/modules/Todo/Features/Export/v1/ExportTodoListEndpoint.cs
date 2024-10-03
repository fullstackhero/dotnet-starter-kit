using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Todo.Features.Export.v1;

public static class ExportTodoListEndpoint
{
    internal static RouteHandlerBuilder MapExportTodoListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/export", async (ISender mediator, [FromBody] BaseFilter filter) =>
        {
            var response = await mediator.Send(new ExportTodoListRequest(filter));
            return Results.Ok(response);
        })
        .WithName(nameof(ExportTodoListEndpoint))
        .WithSummary("Exports a list of todo items")
        .WithDescription("Gets a list of todo items with filtering support")
        .Produces <byte[]>()
        .RequirePermission("Permissions.Todos.Export")
        .MapToApiVersion(1);
    }
}
