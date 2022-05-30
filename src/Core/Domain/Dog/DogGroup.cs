namespace FSH.WebApi.Domain.Dog;
public class DogGroup : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public DogGroup() { }
    public DogGroup(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public DogGroup Update(string? name, string? description)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;

        return this;
    }
}