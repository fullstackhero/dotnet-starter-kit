namespace FSH.WebApi.Application.Dogs;
public class DogColorDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool? IsStandard { get; set; }
    public string? RegistrationCode { get; set; }
}
