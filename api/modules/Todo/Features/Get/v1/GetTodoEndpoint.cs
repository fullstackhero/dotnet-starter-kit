using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Todo.Features.Get.v1;
public static class GetTodoEndpoint
{
    internal static RouteHandlerBuilder MapGetTodoEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}", (Guid id, ISender mediator) => mediator.Send(new GetTodoRequest(id)))
                        .WithName(nameof(GetTodoEndpoint))
                        .MapToApiVersion(1.0);
    }
}
