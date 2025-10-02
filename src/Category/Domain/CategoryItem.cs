using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Domain.Events;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;

namespace Category.Domain;
 
public sealed class CategoryItem : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private CategoryItem() { }

    private CategoryItem(string name, string description)
    {
        Name = name;
        Description = description;
        QueueDomainEvent(new CategoryItemCreated(Id, Name, Description)); 
    }

    public static CategoryItem Create(string name, string description) => new(name, description);

    public CategoryItem Update(string? name, string? description)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(name) && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
        {
            Name = name;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(description) && !string.Equals(Description, description, StringComparison.OrdinalIgnoreCase))
        {
            Description = description;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new CategoryItemUpdated(this));
        }

        return this;
    }
}
