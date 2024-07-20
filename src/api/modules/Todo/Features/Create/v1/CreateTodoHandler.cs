using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Todo.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Todo.Features.Create.v1;
public sealed class CreateTodoHandler(
    ILogger<CreateTodoHandler> logger,
    [FromKeyedServices("todo")] IRepository<TodoItem> repository)
    : IRequestHandler<CreateTodoCommand, CreateTodoResponse>
{
    public async Task<CreateTodoResponse> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = TodoItem.Create(request.Title, request.Note);
        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("todo item created {TodoItemId}", item.Id);
        return new CreateTodoResponse(item.Id);
    }
}
