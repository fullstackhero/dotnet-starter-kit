using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;

public class GetGeoAdminUnitRequest : IRequest<GeoAdminUnitDetailsDto>
{
    public DefaultIdType Id { get; set; }
    public GetGeoAdminUnitRequest(DefaultIdType id) => Id = id;
}

public class GetGeoAdminUnitRequestHandler : IRequestHandler<GetGeoAdminUnitRequest, GeoAdminUnitDetailsDto>
{
    private readonly IRepository<GeoAdminUnit> _repository;
    private readonly IStringLocalizer _t;

    public GetGeoAdminUnitRequestHandler(IRepository<GeoAdminUnit> repository, IStringLocalizer<GetGeoAdminUnitRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<GeoAdminUnitDetailsDto> Handle(GetGeoAdminUnitRequest request, CancellationToken cancellationToken) =>
        await _repository.FirstOrDefaultAsync(
            (ISpecification<GeoAdminUnit, GeoAdminUnitDetailsDto>)new GeoAdminUnitByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);
}