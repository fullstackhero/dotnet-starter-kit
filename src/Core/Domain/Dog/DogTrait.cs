namespace FSH.WebApi.Domain.Dog;
public class DogTrait : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public List<DogBreed>? Breeds { get; set; } = new();

    public DogTrait Update(string name)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        return this;
    }
}
