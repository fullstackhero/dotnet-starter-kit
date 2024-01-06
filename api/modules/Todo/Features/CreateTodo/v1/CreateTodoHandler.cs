using FSH.Framework.Core.Abstraction.Persistence;
using FSH.WebApi.Todo.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Features.CreateTodo.v1;
public sealed class CreateTodoHandler(ILogger<CreateTodoHandler> logger, IRepository<TodoItem> repository) : IRequestHandler<CreateTodoCommand, CreateTodoRepsonse>
{
    public async Task<CreateTodoRepsonse> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(request);
        var item = TodoItem.Create(request.Title, request.Note);
        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("todo item created {TodoItemId}", item.Id);
        return new CreateTodoRepsonse(item.Id);
    }
}
