using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Todo.Features.Search.v1;
public record SearchTodoListRequest(PaginationFilter Filter) : IRequest<PagedList<TodoDto>>;
