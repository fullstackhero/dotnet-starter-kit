using FSH.WebApi.Todo.Data;
using FSH.WebApi.Todo.Models;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public sealed class TodoCreationHandler(ILogger<TodoCreationHandler> logger, TodoDbContext context) : IRequestHandler<TodoCreationCommand, TodoCreationRepsonse>
{
    public async Task<TodoCreationRepsonse> Handle(TodoCreationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = request.Adapt<TodoItem>();
        context.Todos.Add(item);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("todo item created {TodoItemId}", item.Id);
        return new TodoCreationRepsonse(item.Id);
    }
}
