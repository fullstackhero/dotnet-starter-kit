namespace FSH.WebApi.Application.Catalog.GameTypes;

public class GetGameTypeRequest : IRequest<GameTypeDto>
{
    public Guid Id { get; set; }

    public GetGameTypeRequest(Guid id) => Id = id;
}

public class GameTypeByIdSpec : Specification<GameType, GameTypeDto>, ISingleResultSpecification
{
    public GameTypeByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetGameTypeRequestHandler : IRequestHandler<GetGameTypeRequest, GameTypeDto>
{
    private readonly IRepository<GameType> _repository;
    private readonly IStringLocalizer _t;

    public GetGameTypeRequestHandler(IRepository<GameType> repository, IStringLocalizer<GetGameTypeRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<GameTypeDto> Handle(GetGameTypeRequest request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<GameType, GameTypeDto>)new GameTypeByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["GameType {0} Not Found.", request.Id]);
}