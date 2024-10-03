using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Todo.Domain;
using FSH.Starter.WebApi.Todo.Features.Search.v1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Todo.Features.GetList.v1;

public class GetTodoListHandler(
    [FromKeyedServices("todo")] IReadRepository<TodoItem> repository)
    : IRequestHandler<GetTodoListRequest, List<TodoDto>>
{
    public async Task<List<TodoDto>> Handle(GetTodoListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByBaseFilterSpec<TodoItem, TodoDto>(request.Filter);

        return await repository.ListAsync(spec, cancellationToken);
    }
}
