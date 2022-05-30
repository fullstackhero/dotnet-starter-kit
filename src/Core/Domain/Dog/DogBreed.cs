namespace FSH.WebApi.Domain.Dog;
public class DogBreed : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public List<DogTrait>? Traits { get; private set; }
    public string? About { get; private set; }
    public List<DogColor>? Colors { get; private set; }

    public DogBreed() { }
    public DogBreed(string name, List<DogTrait>? traits, string? about, List<DogColor>? colors)
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
    }
    public DogBreed Update(string name, List<DogTrait>? traits, string? about, List<DogColor>? colors)
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

        return this;
    }
}
