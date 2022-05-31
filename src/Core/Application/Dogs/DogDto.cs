namespace FSH.WebApi.Application.Dogs;
public class DogDto : IDto
{
    public Guid Id { get; set; }
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
}
