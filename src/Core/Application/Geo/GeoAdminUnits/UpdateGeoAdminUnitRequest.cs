using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;

public class UpdateGeoAdminUnitRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }

    public int Order { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public string? FullName { get; set; }
    public string? NativeName { get; set; }
    public string? FullNativeName { get; set; }
    public int Grade { get; set; }

    public GeoAdminUnitType Type { get; set; }
}

public class UpdateGeoAdminUnitRequestHandler : IRequestHandler<UpdateGeoAdminUnitRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<GeoAdminUnit> _repository;
    private readonly IStringLocalizer _t;

    public UpdateGeoAdminUnitRequestHandler(IRepositoryWithEvents<GeoAdminUnit> repository, IStringLocalizer<UpdateGeoAdminUnitRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<DefaultIdType> Handle(UpdateGeoAdminUnitRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);

        entity.Update(
            request.Code,
            request.Name,
            request.FullName,
            request.NativeName,
            request.FullNativeName,
            request.Description,
            request.Grade,
            request.Type,
            request.Order,
            request.IsActive);

        await _repository.UpdateAsync(entity, cancellationToken);

        return request.Id;
    }
}