using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Todo.Models;

namespace Todo.Features.TodoItemCreation.v1;
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
