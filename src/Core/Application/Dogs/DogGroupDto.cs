namespace FSH.WebApi.Application.Dogs;
public class DogGroupDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
}
