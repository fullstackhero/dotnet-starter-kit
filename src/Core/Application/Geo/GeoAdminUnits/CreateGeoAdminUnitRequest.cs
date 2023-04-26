using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;
public class CreateGeoAdminUnitRequest : IRequest<DefaultIdType>
{
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

public class CreateGeoAdminUnitRequestHandler : IRequestHandler<CreateGeoAdminUnitRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<GeoAdminUnit> _repository;

    public CreateGeoAdminUnitRequestHandler(IRepositoryWithEvents<GeoAdminUnit> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateGeoAdminUnitRequest request, CancellationToken cancellationToken)
    {
        var entity = new GeoAdminUnit(
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

        await _repository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }
}
