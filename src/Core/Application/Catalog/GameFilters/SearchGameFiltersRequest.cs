namespace FSH.WebApi.Application.Catalog.GameFilters;

public class SearchGameFiltersRequest : PaginationFilter, IRequest<PaginationResponse<GameFilterDto>>
{
}

public class GameFiltersBySearchRequestSpec : EntitiesByPaginationFilterSpec<GameFilter, GameFilterDto>
{
    public GameFiltersBySearchRequestSpec(SearchGameFiltersRequest request)
        : base(request) =>
        Query.OrderBy(c => c.Name, !request.HasOrderBy());
}

public class SearchGameFiltersRequestHandler : IRequestHandler<SearchGameFiltersRequest, PaginationResponse<GameFilterDto>>
{
    private readonly IReadRepository<GameFilter> _repository;

    public SearchGameFiltersRequestHandler(IReadRepository<GameFilter> repository) => _repository = repository;

    public async Task<PaginationResponse<GameFilterDto>> Handle(SearchGameFiltersRequest request, CancellationToken cancellationToken)
    {
        var spec = new GameFiltersBySearchRequestSpec(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}