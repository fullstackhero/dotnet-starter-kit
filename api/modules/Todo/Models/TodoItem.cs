using FSH.Framework.Domain;

namespace Todo.Models;
public class TodoItem : AuditableEntity
{
    public string? Title { get; set; }

    public string? Note { get; set; }
}
