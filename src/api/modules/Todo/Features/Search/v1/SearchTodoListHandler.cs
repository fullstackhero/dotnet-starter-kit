using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Todo.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Todo.Features.Search.v1;

public sealed class SearchTodoListHandler(
    [FromKeyedServices("todo")] IReadRepository<TodoItem> repository)
    : IRequestHandler<SearchTodoListRequest, PagedList<TodoDto>>
{
    public async Task<PagedList<TodoDto>> Handle(SearchTodoListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByPaginationFilterSpec<TodoItem, TodoDto>(request.Filter);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<TodoDto>(items, request.Filter.PageNumber, request.Filter.PageSize, totalCount);
    }
}
