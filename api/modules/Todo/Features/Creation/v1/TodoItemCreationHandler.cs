using FSH.WebApi.Todo.Models;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public sealed class TodoItemCreationHandler(ILogger<TodoItemCreationHandler> logger) : IRequestHandler<TodoItemCreationCommand, Guid>
{
    public async Task<Guid> Handle(TodoItemCreationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await Task.FromResult(0).ConfigureAwait(false);
        var item = request.Adapt<TodoItem>();
        logger.LogInformation("todo item created {TodoItemId}", item.Id);
        return item.Id;
    }
}
