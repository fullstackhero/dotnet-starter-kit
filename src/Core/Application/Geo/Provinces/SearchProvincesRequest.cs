using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces;

public class SearchProvincesRequest : PaginationFilter, IRequest<PaginationResponse<ProvinceDto>>
{
    public DefaultIdType? TypeId { get;  set; }
    public DefaultIdType? StateId { get; set; }

}

public class SearchProvincesRequestHandler : IRequestHandler<SearchProvincesRequest, PaginationResponse<ProvinceDto>>
{
    private readonly IReadRepository<Province> _repository;

    public SearchProvincesRequestHandler(IReadRepository<Province> repository) => _repository = repository;

    public async Task<PaginationResponse<ProvinceDto>> Handle(SearchProvincesRequest request, CancellationToken cancellationToken)
    {
        var spec = new SearchProvincesSpecification(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class SearchProvincesSpecification : EntitiesByPaginationFilterSpec<Province, ProvinceDto>
{
    public SearchProvincesSpecification(SearchProvincesRequest request)
        : base(request) =>
            Query
                .OrderBy(e => e.Order, !request.HasOrderBy())
                .Where(e => e.TypeId.Equals(request.TypeId!.Value), request.TypeId.HasValue)
                .Where(e => e.StateId.Equals(request.StateId!.Value), request.StateId.HasValue);
}