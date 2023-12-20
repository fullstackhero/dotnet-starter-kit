using FSH.WebApi.Todo.Data;
using FSH.WebApi.Todo.Models;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public sealed class TodoItemCreationHandler(ILogger<TodoItemCreationHandler> logger, TodoDbContext context) : IRequestHandler<TodoItemCreationCommand, Guid>
{
    public async Task<Guid> Handle(TodoItemCreationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = request.Adapt<TodoItem>();
        context.Todos.Add(item);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("todo item created {TodoItemId}", item.Id);
        return item.Id;
    }
}
