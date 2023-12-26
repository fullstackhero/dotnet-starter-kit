using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;

namespace FSH.WebApi.Todo.Models;
public class TodoItem : AuditableEntity, IAggregateRoot
{
    public string? Title { get; set; }

    public string? Note { get; set; }
}
