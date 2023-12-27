using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.WebApi.Todo.Features.GetList.v1;
public record GetTodoListRequest(int PageNumber, int PageSize) : IRequest<PagedList<TodoDto>>;
