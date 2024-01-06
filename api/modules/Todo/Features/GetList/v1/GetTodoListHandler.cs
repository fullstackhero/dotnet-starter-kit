using FSH.Framework.Core.Abstraction.Persistence;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.WebApi.Todo.Models;
using MediatR;

namespace FSH.WebApi.Todo.Features.GetList.v1;
public sealed class GetTodoListHandler(IReadRepository<TodoItem> repository) : IRequestHandler<GetTodoListRequest, PagedList<TodoDto>>
{
    public async Task<PagedList<TodoDto>> Handle(GetTodoListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new ListSpecification<TodoItem, TodoDto>(request.PageNumber, request.PageSize);
        var items = await repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken).ConfigureAwait(false);
        return items;
    }
}
