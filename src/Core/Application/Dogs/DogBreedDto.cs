namespace FSH.WebApi.Application.Dogs;
public class DogBreedDto : IDto
{
    public string Name { get; set; }
    public List<DogTraitDto>? Traits { get; set; }
    public string? About { get; set; }
    public List<DogColorDto>? Colors { get; set; }
    public DogGroup? Group { get; set; }
}
