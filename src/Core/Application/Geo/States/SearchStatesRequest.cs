using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States;

public class SearchStatesRequest : PaginationFilter, IRequest<PaginationResponse<StateDto>>
{
    public DefaultIdType? TypeId { get;  set; }
    public DefaultIdType? CountryId { get; set; }

}

public class SearchStatesRequestHandler : IRequestHandler<SearchStatesRequest, PaginationResponse<StateDto>>
{
    private readonly IReadRepository<State> _repository;

    public SearchStatesRequestHandler(IReadRepository<State> repository) => _repository = repository;

    public async Task<PaginationResponse<StateDto>> Handle(SearchStatesRequest request, CancellationToken cancellationToken)
    {
        var spec = new SearchStatesSpecification(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class SearchStatesSpecification : EntitiesByPaginationFilterSpec<State, StateDto>
{
    public SearchStatesSpecification(SearchStatesRequest request)
        : base(request) =>
            Query
                .OrderBy(e => e.Order, !request.HasOrderBy())
                .Where(e => e.TypeId.Equals(request.TypeId!.Value), request.TypeId.HasValue)
                .Where(e => e.CountryId.Equals(request.CountryId!.Value), request.CountryId.HasValue);
}