namespace FSH.WebApi.Domain.Dog;
public class DogBreed : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public List<DogTrait>? Traits { get; set; }
    public string? About { get; set; }
    public List<DogColor>? Colors { get; set; }
    public Guid? DogGroupId { get; set; }
    public DogGroup? Group { get; set; }
    public List<Dog> Dogs { get; set; }

    public DogBreed()
    {
    }

    public DogBreed(string name, List<DogTrait>? traits, string? about, List<DogColor>? colors, DogGroup? group)
    {
        Name = name;
        Traits = new List<DogTrait>();
        if (traits?.Count > 0)
        {
            Traits.AddRange(traits);
        }

        About = about;
        Colors = new List<DogColor>();
        if (colors?.Count > 0)
        {
            Colors.AddRange(colors);
        }

        Group = group;
    }

    public DogBreed Update(string name, List<DogTrait>? traits, string? about, List<DogColor>? colors, DogGroup group)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (traits?.Count > 0)
        {
            if (Traits == null)
            {
                Traits = new List<DogTrait>();
                Traits.AddRange(traits);
            }
            else
            {
                Traits.Clear();
                Traits.AddRange(traits);
            }
        }

        if (about is not null && About?.Equals(about) is not true) About = about;
        if (colors?.Count > 0)
        {
            if (Colors == null)
            {
                Colors = new List<DogColor>();
                Colors.AddRange(colors);
            }
            else
            {
                Colors.Clear();
                Colors.AddRange(colors);
            }
        }

        if (group is not null && Group?.Name.Equals(group?.Name) is not true) Group = group;

        return this;
    }
}
