namespace FSH.WebApi.Application.Catalog.Filters;

public class SearchFiltersRequest : PaginationFilter, IRequest<PaginationResponse<FilterDto>>
{
}

public class FiltersBySearchRequestSpec : EntitiesByPaginationFilterSpec<Filter, FilterDto>
{
    public FiltersBySearchRequestSpec(SearchFiltersRequest request)
        : base(request) =>
        Query.OrderBy(c => c.Name, !request.HasOrderBy());
}

public class SearchFiltersRequestHandler : IRequestHandler<SearchFiltersRequest, PaginationResponse<FilterDto>>
{
    private readonly IReadRepository<Filter> _repository;

    public SearchFiltersRequestHandler(IReadRepository<Filter> repository) => _repository = repository;

    public async Task<PaginationResponse<FilterDto>> Handle(SearchFiltersRequest request, CancellationToken cancellationToken)
    {
        var spec = new FiltersBySearchRequestSpec(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}