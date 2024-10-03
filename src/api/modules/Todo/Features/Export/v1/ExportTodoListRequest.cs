using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Todo.Features.Export.v1;
public record ExportTodoListRequest(BaseFilter Filter)  : IRequest<byte[]>;
