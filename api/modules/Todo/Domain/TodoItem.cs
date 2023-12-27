using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.WebApi.Todo.Domain.Events;

namespace FSH.WebApi.Todo.Models;
public class TodoItem : AuditableEntity, IAggregateRoot
{
    public string? Title { get; set; }

    public string? Note { get; set; }

    public static TodoItem Create(string title, string note)
    {
        var item = new TodoItem();

        item.Title = title;
        item.Note = note;

        item.QueueDomainEvent(new TodoItemCreated(item.Id, item.Title));

        return item;
    }
}
