using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;
namespace FSH.WebApi.Application.Geo.Districts;

public class CreateDistrictRequest : IRequest<DefaultIdType>
{
    public int Order { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public string? FullName { get; set; }
    public string? NativeName { get; set; }
    public string? FullNativeName { get; set; }
    public int? NumericCode { get; set; }

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    public DefaultIdType? TypeId { get; set; }
    public DefaultIdType ProvinceId { get; set; }
}

public class CreateDistrictRequestHandler : IRequestHandler<CreateDistrictRequest, DefaultIdType>
{
    private readonly IRepository<District> _repository;
    private readonly IFileStorageService _file;

    public CreateDistrictRequestHandler(IRepository<District> repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    public async Task<DefaultIdType> Handle(CreateDistrictRequest request, CancellationToken cancellationToken)
    {

        var entity = new District(
            request.Order,
            request.Code,
            request.Name,
            request.Description,
            request.IsActive,
            request.FullName,
            request.NativeName,
            request.FullNativeName,
            request.NumericCode,
            request.Latitude,
            request.Longitude,
            request.TypeId,
            request.ProvinceId);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }
}
