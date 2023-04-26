using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class SearchCountriesRequest : PaginationFilter, IRequest<PaginationResponse<CountryDto>>
{
    public DefaultIdType? TypeId { get;  set; }
    public DefaultIdType? SubTypeId { get; set; }
    public DefaultIdType? ContinentId { get; set; }
    public DefaultIdType? SubContinentId { get; set; }

}

public class SearchCountriesRequestHandler : IRequestHandler<SearchCountriesRequest, PaginationResponse<CountryDto>>
{
    private readonly IReadRepository<Country> _repository;

    public SearchCountriesRequestHandler(IReadRepository<Country> repository) => _repository = repository;

    public async Task<PaginationResponse<CountryDto>> Handle(SearchCountriesRequest request, CancellationToken cancellationToken)
    {
        var spec = new SearchCountriesSpecification(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class SearchCountriesSpecification : EntitiesByPaginationFilterSpec<Country, CountryDto>
{
    public SearchCountriesSpecification(SearchCountriesRequest request)
        : base(request) =>
            Query
                .OrderBy(e => e.Order, !request.HasOrderBy())
                .Where(e => e.TypeId.Equals(request.TypeId!.Value), request.TypeId.HasValue)

                // .Where(e => e.SubTypeId.Equals(request.SubTypeId!.Value), request.SubTypeId.HasValue)
                // .Where(e => e.ContinentId.Equals(request.ContinentId!.Value), request.ContinentId.HasValue)
                // .Where(e => e.SubContinentId.Equals(request.SubContinentId!.Value), request.SubContinentId.HasValue)
                ;
}