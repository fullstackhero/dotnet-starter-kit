using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards;

public class SearchWardsRequest : PaginationFilter, IRequest<PaginationResponse<WardDto>>
{
    public DefaultIdType? TypeId { get;  set; }
    public DefaultIdType? DistrictId { get; set; }

}

public class SearchWardsRequestHandler : IRequestHandler<SearchWardsRequest, PaginationResponse<WardDto>>
{
    private readonly IReadRepository<Ward> _repository;

    public SearchWardsRequestHandler(IReadRepository<Ward> repository) => _repository = repository;

    public async Task<PaginationResponse<WardDto>> Handle(SearchWardsRequest request, CancellationToken cancellationToken)
    {
        var spec = new SearchWardsSpecification(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class SearchWardsSpecification : EntitiesByPaginationFilterSpec<Ward, WardDto>
{
    public SearchWardsSpecification(SearchWardsRequest request)
        : base(request) =>
            Query
                .OrderBy(e => e.Order, !request.HasOrderBy())
                .Where(e => e.TypeId.Equals(request.TypeId!.Value), request.TypeId.HasValue)
                .Where(e => e.DistrictId.Equals(request.DistrictId!.Value), request.DistrictId.HasValue);
}