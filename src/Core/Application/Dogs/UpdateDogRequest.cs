namespace FSH.WebApi.Application.Dogs;
public class UpdateDogRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OfficialName { get; set; }
    public string? AkcId { get; set; }
    public DateTime? Birthdate { get; set; }
    public Guid? DogBreedId { get; set; }
    public string? Gender { get; set; }
    public string? Microchip { get; set; }
    public string? ImagePath { get; set; }
    public Guid? DogColorId { get; set; }
}

public class UpdateDogRequestValidator : CustomValidator<UpdateDogRequest>
{
    public UpdateDogRequestValidator(IRepository<Dog> repository, IStringLocalizer<UpdateDogRequestValidator> T) =>
       RuleFor(p => p.Name)
           .NotEmpty()
           .MaximumLength(75)
           .MustAsync(async (dog, name, ct) =>
                   await repository.GetBySpecAsync(new DogByNameSpec(name), ct)
                       is not Dog existingDog || existingDog.Id == dog.Id)
               .WithMessage((_, name) => T["Dog {0} already Exists.", name]);
}

public class UpdateDogRequestHandler : IRequestHandler<UpdateDogRequest, Guid>
{
    // Add Domian Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Dog> _repository;
    private readonly IStringLocalizer _t;

    public UpdateDogRequestHandler(IRepositoryWithEvents<Dog> repository, IStringLocalizer<UpdateDogRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(UpdateDogRequest request, CancellationToken cancellationToken)
    {
        var dog = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = dog ?? throw new NotFoundException(_t[$"Dog {request.Id} Not Found"]);

        dog.Update(
            request.Name,
            request.OfficialName,
            request.AkcId,
            request.Birthdate,
            request.DogBreedId,
            request.Gender,
            request.Microchip,
            request.ImagePath,
            request.DogColorId);

        await _repository.UpdateAsync(dog, cancellationToken);

        return request.Id;
    }
}
