using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Todo.Features.Search.v1;
using MediatR;

namespace FSH.Starter.WebApi.Todo.Features.GetList.v1;

public record GetTodoListRequest(BaseFilter Filter) : IRequest<List<TodoDto>>;
