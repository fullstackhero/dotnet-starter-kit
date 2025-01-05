﻿using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Todo.Features.GetList.v1;
public record GetTodoListRequest(PaginationFilter Filter) : IRequest<PagedList<TodoDto>>;
