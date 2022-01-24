namespace FSH.WebApi.Domain.Catalog;

public class ProductCategory : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? Icon { get; private set; }
    public Guid BrandId { get; private set; }
    public virtual Brand Brand { get; private set; } = default!;

    public ProductCategory(string name, string? description,  string? icon)
    {
        Name = name;
        Description = description;
        Icon = icon;
    }

    public ProductCategory Update(string? name, string? description,  string? icon)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
      
        if (Icon is not null && Icon?.Equals(Icon) is not true) Icon = icon;
        return this;
    }
}