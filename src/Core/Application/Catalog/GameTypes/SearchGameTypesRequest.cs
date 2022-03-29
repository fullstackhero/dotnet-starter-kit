namespace FSH.WebApi.Application.Catalog.GameTypes;

public class SearchGameTypesRequest : PaginationFilter, IRequest<PaginationResponse<GameTypeDto>>
{
}

public class GameTypesBySearchRequestSpec : EntitiesByPaginationFilterSpec<GameType, GameTypeDto>
{
    public GameTypesBySearchRequestSpec(SearchGameTypesRequest request)
        : base(request) =>
        Query.OrderBy(c => c.Name, !request.HasOrderBy());
}

public class SearchGameTypesRequestHandler : IRequestHandler<SearchGameTypesRequest, PaginationResponse<GameTypeDto>>
{
    private readonly IReadRepository<GameType> _repository;

    public SearchGameTypesRequestHandler(IReadRepository<GameType> repository) => _repository = repository;

    public async Task<PaginationResponse<GameTypeDto>> Handle(SearchGameTypesRequest request, CancellationToken cancellationToken)
    {
        var spec = new GameTypesBySearchRequestSpec(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}