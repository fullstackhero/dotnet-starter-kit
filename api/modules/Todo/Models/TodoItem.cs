using FSH.Framework.Core.Domain;

namespace FSH.WebApi.Todo.Models;
public class TodoItem : AuditableEntity
{
    public string? Title { get; set; }

    public string? Note { get; set; }
}
