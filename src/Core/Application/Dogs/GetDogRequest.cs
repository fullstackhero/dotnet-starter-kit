namespace FSH.WebApi.Application.Dogs;
public class GetDogRequest : IRequest<DogDto>
{
    public Guid Id { get; set; }
    public GetDogRequest(Guid id) => Id = id;
}

public class DogByIdSpec : Specification<Dog, DogDto>, ISingleResultSpecification
{
    public DogByIdSpec(Guid id) =>
        Query
        .Include(c => c.Color)
        .Include(b => b.Breed)
        .Where(d => d.Id == id);
}

public class GetDogRequestHandler : IRequestHandler<GetDogRequest, DogDto>
{
    private readonly IRepository<Dog> _repository;
    private readonly IStringLocalizer _t;

    public GetDogRequestHandler(IRepository<Dog> repository, IStringLocalizer<GetDogRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<DogDto> Handle(GetDogRequest request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<Dog, DogDto>)new DogByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Dog {0} Not Found.", request.Id]);
}
