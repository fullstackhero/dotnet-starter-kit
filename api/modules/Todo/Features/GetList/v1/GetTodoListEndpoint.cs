using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Todo.Features.GetList.v1;
public static class GetTodoListEndpoint
{
    internal static RouteHandlerBuilder MapGetTodoListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", (int pageNumber, int pageSize, ISender mediator) => mediator.Send(new GetTodoListRequest(pageNumber, pageSize)))
                        .WithName(nameof(GetTodoListEndpoint))
                        .MapToApiVersion(1.0);
    }
}
