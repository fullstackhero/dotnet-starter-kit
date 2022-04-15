namespace FSH.WebApi.Application.Catalog.GameFilters;

public class GetGameFilterRequest : IRequest<GameFilterDto>
{
    public Guid Id { get; set; }

    public GetGameFilterRequest(Guid id) => Id = id;
}

public class GameFilterByIdSpec : Specification<GameFilter, GameFilterDto>, ISingleResultSpecification
{
    public GameFilterByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetGameFilterRequestHandler : IRequestHandler<GetGameFilterRequest, GameFilterDto>
{
    private readonly IRepository<GameFilter> _repository;
    private readonly IStringLocalizer _t;

    public GetGameFilterRequestHandler(IRepository<GameFilter> repository, IStringLocalizer<GetGameFilterRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<GameFilterDto> Handle(GetGameFilterRequest request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<GameFilter, GameFilterDto>)new GameFilterByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["GameFilter {0} Not Found.", request.Id]);
}