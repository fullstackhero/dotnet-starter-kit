namespace FSH.WebApi.Domain.Dog;
public class DogTrait : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;

    public DogTrait() { }
    public DogTrait(string name)
    {
        Name = name;
    }

    public DogTrait Update(string name)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        return this;
    }
}
