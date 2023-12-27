using FSH.Framework.Core.Persistence;
using FSH.WebApi.Todo.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public sealed class TodoCreationHandler(ILogger<TodoCreationHandler> logger, IRepository<TodoItem> repository) : IRequestHandler<TodoCreationCommand, TodoCreationRepsonse>
{
    public async Task<TodoCreationRepsonse> Handle(TodoCreationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = TodoItem.Create(request.Title, request.Note);
        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("todo item created {TodoItemId}", item.Id);
        return new TodoCreationRepsonse(item.Id);
    }
}
