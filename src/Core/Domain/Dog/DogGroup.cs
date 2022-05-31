namespace FSH.WebApi.Domain.Dog;
public class DogGroup : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<DogBreed>? DogBreeds { get; set; }

    public DogGroup Update(string? name, string? description)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;

        return this;
    }
}