using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Todo.Domain;
using FSH.Starter.WebApi.Todo.Features.Search.v1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Todo.Features.Export.v1;

public class ExportTodoListHandler(
    [FromKeyedServices("todo")] IReadRepository<TodoItem> repository, IDataExport dataExport)
    : IRequestHandler<ExportTodoListRequest, byte[]>
{
    public async Task<byte[]> Handle(ExportTodoListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByBaseFilterSpec<TodoItem, TodoDto>(request.Filter);

        var items = await repository.ListAsync(spec, cancellationToken);
        
        var response = dataExport.ListToByteArray(items);
        
        return response;
    }

}
