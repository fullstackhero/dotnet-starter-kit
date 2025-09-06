using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Todo.Domain;
using FSH.Starter.WebApi.Todo.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Todo.Features.Update.v1;
public sealed class UpdateTodoHandler(
    ILogger<UpdateTodoHandler> logger,
    [FromKeyedServices("todo")] IRepository<TodoItem> repository)
    : IRequestHandler<UpdateTodoCommand, UpdateTodoResponse>
{
    public async Task<UpdateTodoResponse> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = todo ?? throw new TodoItemNotFoundException(request.Id);
        var updatedTodo = todo.Update(request.Title, request.Note);
        await repository.UpdateAsync(updatedTodo, cancellationToken);
        logger.LogInformation("todo item updated {TodoItemId}", updatedTodo.Id);
        return new UpdateTodoResponse(updatedTodo.Id);
    }
}
