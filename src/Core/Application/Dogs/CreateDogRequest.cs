namespace FSH.WebApi.Application.Dogs;
public class CreateDogRequest : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? OfficialName { get; set; }
    public string? AkcId { get; set; }
    public DateTime Birthdate { get; set; }
    public DogBreed? Breed { get; set; }
    public string? Gender { get; set; }
    public string? Microchip { get; set; }
    public string? ImagePath { get; set; }
    public DogColor? Color { get; set; }
    public DogGroup? Group { get; set; }
}

public class CreateDogRequestHandler : IRequestHandler<CreateDogRequest, Guid>
{
    // Add domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Dog> _repository;

    public CreateDogRequestHandler(IRepositoryWithEvents<Dog> repository) => _repository = repository;

    public async Task<Guid> Handle(CreateDogRequest request, CancellationToken cancellationToken)
    {
        var dog = new Dog(
            request.Name,
            request.OfficialName,
            request.AkcId,
            request.Birthdate,
            request.Breed,
            request.Gender,
            request.Microchip,
            request.ImagePath,
            request.Color,
            request.Group
            );

        await _repository.AddAsync(dog, cancellationToken);

        return dog.Id;
    }
}



