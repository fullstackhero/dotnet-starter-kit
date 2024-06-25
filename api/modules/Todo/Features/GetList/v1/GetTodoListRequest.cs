using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.WebApi.Todo.Features.GetList.v1;
public record GetTodoListRequest(PaginationFilter filter) : IRequest<PagedList<TodoDto>>;
