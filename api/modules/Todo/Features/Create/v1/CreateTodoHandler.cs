using FSH.Framework.Core.Persistence;
using FSH.WebApi.Todo.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Features.CreateTodo.v1;
public sealed class CreateTodoHandler(
    ILogger<CreateTodoHandler> logger,
    [FromKeyedServices("todo")] IRepository<TodoItem> repository)
    : IRequestHandler<CreateTodoCommand, CreateTodoRepsonse>
{
    public async Task<CreateTodoRepsonse> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = TodoItem.Create(request.Title, request.Note);
        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("todo item created {TodoItemId}", item.Id);
        return new CreateTodoRepsonse(item.Id);
    }
}
