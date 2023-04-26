using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;

public class SearchGeoAdminUnitsRequest : PaginationFilter, IRequest<PaginationResponse<GeoAdminUnitDto>>
{
    public GeoAdminUnitType? Type { get;  set; }
}

public class SearchGeoAdminUnitsRequestHandler : IRequestHandler<SearchGeoAdminUnitsRequest, PaginationResponse<GeoAdminUnitDto>>
{
    private readonly IReadRepository<GeoAdminUnit> _repository;

    public SearchGeoAdminUnitsRequestHandler(IReadRepository<GeoAdminUnit> repository) => _repository = repository;

    public async Task<PaginationResponse<GeoAdminUnitDto>> Handle(SearchGeoAdminUnitsRequest request, CancellationToken cancellationToken)
    {
        var spec = new SearchGeoAdminUnitsSpecification(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class SearchGeoAdminUnitsSpecification : EntitiesByPaginationFilterSpec<GeoAdminUnit, GeoAdminUnitDto>
{
    public SearchGeoAdminUnitsSpecification(SearchGeoAdminUnitsRequest request)
        : base(request) =>
            Query
                .OrderBy(e => e.Order, !request.HasOrderBy())
                .Where(e => e.Type.Equals(request.Type!.Value), request.Type.HasValue);
}