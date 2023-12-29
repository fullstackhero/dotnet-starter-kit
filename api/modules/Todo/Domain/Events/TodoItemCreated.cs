
using FSH.Framework.Core.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Todo.Domain.Events;
public record TodoItemCreated(Guid Id, string Title) : DomainEvent;

public class TodoItemCreatedEventHandler(ILogger<TodoItemCreatedEventHandler> logger) : INotificationHandler<TodoItemCreated>
{
    public Task Handle(TodoItemCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling todo item created domain event..");
        logger.LogInformation("finished handling todo item created domain event..");
        return Task.CompletedTask;
    }
}
