namespace FSH.WebApi.Domain.Dog;
public class Dog : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? OfficialName { get; set; }
    public string? AkcId { get; set; }
    public DateTime? Birthdate { get; set; }
    public Guid? DogBreedId { get; set; }
    public DogBreed? Breed { get; set; }
    public string? Gender { get; set; }
    public string? Microchip { get; set; }
    public string? ImagePath { get; set; }
    public Guid? DogColorId { get; set; }
    public DogColor? Color { get; set; }

    public Dog()
    {
    }

    public Dog(
        string name,
        string? officialname,
        string? akcid,
        DateTime? birthdate,
        Guid? breedid,
        string? gender,
        string? microchip,
        string? imagepath,
        Guid? colorid)
    {
        Name = name;
        OfficialName = officialname;
        AkcId = akcid;
        Birthdate = birthdate;
        DogBreedId = breedid;
        Gender = gender;
        Microchip = microchip;
        ImagePath = imagepath;
        DogColorId = colorid;
    }

    public Dog Update(
        string name,
        string? officialname,
        string? akcid,
        DateTime? birthdate,
        Guid? breedid,
        string? gender,
        string? microchip,
        string? imagepath,
        Guid? colorid)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (officialname is not null && OfficialName?.Equals(officialname) is not true) OfficialName = officialname;
        if (akcid is not null && AkcId?.Equals(akcid) is not true) AkcId = akcid;
        if (birthdate is not null && !Birthdate.Equals(birthdate)) Birthdate = birthdate;
        if (breedid is not null && DogBreedId.Equals(breedid) is not true) DogBreedId = breedid;
        if (gender is not null && Gender?.Equals(gender) is not true) Gender = gender;
        if (microchip is not null && Microchip?.Equals(microchip) is not true) Microchip = microchip;
        if (imagepath is not null && ImagePath?.Equals(imagepath) is not true) ImagePath = imagepath;
        if (colorid is not null && DogColorId.Equals(colorid) is not true) DogColorId = colorid;

        return this;
    }
}
