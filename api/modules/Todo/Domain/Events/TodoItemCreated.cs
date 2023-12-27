
using FSH.Framework.Core.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Domain.Events;
public record TodoItemCreated(Guid Id, string Title) : DomainEvent;

public class TodoItemCreatedHandler(ILogger<TodoItemCreatedHandler> logger) : INotificationHandler<TodoItemCreated>
{
    public async Task Handle(TodoItemCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling todo item created domain event..");
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("finished handling todo item created domain event..");
    }
}
