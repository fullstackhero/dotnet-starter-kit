using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts;

public class SearchDistrictsRequest : PaginationFilter, IRequest<PaginationResponse<DistrictDto>>
{
    public DefaultIdType? TypeId { get;  set; }
    public DefaultIdType? ProvinceId { get; set; }

}

public class SearchDistrictsRequestHandler : IRequestHandler<SearchDistrictsRequest, PaginationResponse<DistrictDto>>
{
    private readonly IReadRepository<District> _repository;

    public SearchDistrictsRequestHandler(IReadRepository<District> repository) => _repository = repository;

    public async Task<PaginationResponse<DistrictDto>> Handle(SearchDistrictsRequest request, CancellationToken cancellationToken)
    {
        var spec = new SearchDistrictsSpecification(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class SearchDistrictsSpecification : EntitiesByPaginationFilterSpec<District, DistrictDto>
{
    public SearchDistrictsSpecification(SearchDistrictsRequest request)
        : base(request) =>
            Query
                .OrderBy(e => e.Order, !request.HasOrderBy())
                .Where(e => e.TypeId.Equals(request.TypeId!.Value), request.TypeId.HasValue)
                .Where(e => e.ProvinceId.Equals(request.ProvinceId!.Value), request.ProvinceId.HasValue);
}