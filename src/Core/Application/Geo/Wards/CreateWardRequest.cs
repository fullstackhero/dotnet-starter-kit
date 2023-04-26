using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;
namespace FSH.WebApi.Application.Geo.Wards;

public class CreateWardRequest : IRequest<DefaultIdType>
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
    public DefaultIdType DistrictId { get; set; }
}

public class CreateWardRequestHandler : IRequestHandler<CreateWardRequest, DefaultIdType>
{
    private readonly IRepository<Ward> _repository;
    private readonly IFileStorageService _file;

    public CreateWardRequestHandler(IRepository<Ward> repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    public async Task<DefaultIdType> Handle(CreateWardRequest request, CancellationToken cancellationToken)
    {

        var entity = new Ward(
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
            request.DistrictId);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }
}
