namespace FSH.WebApi.Application.Dogs;
public class DogDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? OfficialName { get; set; }
    public string? AkcId { get; set; }
    public DateTime Birthdate { get; set; }

    // public string Age { get; private set; } = string.Empty; should be calculated

    public DogBreedDto? Breed { get; set; }
    public string? Gender { get; set; }
    public string? Microchip { get; set; }
    public string? ImagePath { get; set; }
    public DogColorDto? Color { get; set; }
    public DogGroupDto? Group { get; set; }
}
