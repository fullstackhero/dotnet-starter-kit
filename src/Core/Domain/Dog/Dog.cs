namespace FSH.WebApi.Domain.Dog;
public class Dog : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string? OfficialName { get; private set; }
    public string? AkcId { get; private set; }
    public DateTime Birthdate { get; private set; }

    // public string Age { get; private set; } = string.Empty; should be calculated

    public DogBreed? Breed { get; private set; }
    public string? Gender { get; private set; }
    public string? Microchip { get; private set; }
    public string? ImagePath { get; private set; }
    public DogColor? Color { get; private set; }
    public DogGroup? Group { get; private set; }

    public Dog() { }
    public Dog(
        string name,
        string? officialname,
        string? akcid,
        DateTime birthdate,
        DogBreed? breed,
        string? gender,
        string? microchip,
        string? imagepath,
        DogColor? color,
        DogGroup? group)
    {
        Name = name;
        OfficialName = officialname;
        AkcId = akcid;
        Birthdate = birthdate;
        Breed = breed;
        Gender = gender;
        Microchip = microchip;
        ImagePath = imagepath;
        Color = color;
        Group = group;
    }

    public Dog Update(
        string name,
        string? officialname,
        string? akcid,
        DateTime birthdate,
        DogBreed? breed,
        string? gender,
        string? microchip,
        string? imagepath,
        DogColor? color,
        DogGroup? group)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (officialname is not null && OfficialName?.Equals(officialname) is not true) OfficialName = officialname;
        if (akcid is not null && AkcId?.Equals(akcid) is not true) AkcId = akcid;
        if (!Birthdate.Equals(birthdate)) Birthdate = birthdate;
        if (breed is not null && Breed?.Equals(breed) is not true) Breed = breed;
        if (gender is not null && Gender?.Equals(gender) is not true) Gender = gender;
        if (microchip is not null && Microchip?.Equals(microchip) is not true) Microchip = microchip;
        if (imagepath is not null && ImagePath?.Equals(imagepath) is not true) ImagePath = imagepath;
        if (color is not null && Color?.Equals(color) is not true) Color = color;
        if (group is not null && Group?.Equals(group) is not true) Group = group;

        return this;
    }
}
