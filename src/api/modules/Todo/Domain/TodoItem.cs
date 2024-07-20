using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Todo.Domain.Events;

namespace FSH.Starter.WebApi.Todo.Domain;
public class TodoItem : AuditableEntity, IAggregateRoot
{
    public string? Title { get; set; }

    public string? Note { get; set; }

    public static TodoItem Create(string title, string note)
    {
        var item = new TodoItem();

        item.Title = title;
        item.Note = note;

        item.QueueDomainEvent(new TodoItemCreated(item.Id, item.Title, item.Note));

        TodoMetrics.Created.Add(1);

        return item;
    }

    public TodoItem Update(string? title, string? note)
    {
        if (title is not null && Title?.Equals(title, StringComparison.OrdinalIgnoreCase) is not true) Title = title;
        if (note is not null && Note?.Equals(note, StringComparison.OrdinalIgnoreCase) is not true) Note = note;

        this.QueueDomainEvent(new TodoItemUpdated(this));

        return this;

    }
}
