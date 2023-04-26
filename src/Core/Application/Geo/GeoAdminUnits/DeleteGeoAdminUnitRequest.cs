using FSH.WebApi.Application.Geo.Countries;
using FSH.WebApi.Application.Geo.Districts;
using FSH.WebApi.Application.Geo.Provinces;
using FSH.WebApi.Application.Geo.States;
using FSH.WebApi.Application.Geo.Wards;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;
public class DeleteGeoAdminUnitRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }

    public DeleteGeoAdminUnitRequest(DefaultIdType id) => Id = id;
}

public class DeleteGeoAdminUnitRequestHandler : IRequestHandler<DeleteGeoAdminUnitRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<GeoAdminUnit> _repository;
    private readonly IReadRepository<Country> _countryRepo;
    private readonly IReadRepository<State> _stateRepo;
    private readonly IReadRepository<Province> _provinceRepo;
    private readonly IReadRepository<District> _districtRepo;
    private readonly IReadRepository<Ward> _wardRepo;

    private readonly IStringLocalizer _t;

    public DeleteGeoAdminUnitRequestHandler(
        IRepositoryWithEvents<GeoAdminUnit> repository,
        IReadRepository<Country> countryRepo,
        IReadRepository<State> stateRepo,
        IReadRepository<Province> provinceRepo,
        IReadRepository<District> districtRepo,
        IReadRepository<Ward> wardRepo,
        IStringLocalizer<DeleteGeoAdminUnitRequestHandler> localizer)
        => (_repository, _countryRepo, _stateRepo, _provinceRepo, _districtRepo, _wardRepo, _t)
        = (repository, countryRepo, stateRepo, provinceRepo, districtRepo, wardRepo, localizer);

    public async Task<DefaultIdType> Handle(DeleteGeoAdminUnitRequest request, CancellationToken cancellationToken)
    {
        if (await _countryRepo.AnyAsync(new CountriesByTypesSpec(request.Id), cancellationToken)
            || await _stateRepo.AnyAsync(new StatesByTypeSpec(request.Id), cancellationToken)
            || await _provinceRepo.AnyAsync(new ProvincesByTypeSpec(request.Id), cancellationToken)
            || await _districtRepo.AnyAsync(new DistrictsByTypeSpec(request.Id), cancellationToken)
            || await _wardRepo.AnyAsync(new WardsByTypeSpec(request.Id), cancellationToken)
            )
        {
            throw new ConflictException(_t["Entity cannot be deleted as it's being used."]);
        }

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity ?? throw new NotFoundException(_t["Entity {0} Not Found."]);

        await _repository.DeleteAsync(entity, cancellationToken);

        return request.Id;
    }
}
