using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;
namespace FSH.WebApi.Application.Geo.Provinces;

public class CreateProvinceRequest : IRequest<DefaultIdType>
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
    public string? Metropolis { get; set; }

    public string? ZipCode { get; set; }
    public string? PhoneCode { get; set; }

    public int? Population { get; set; }
    public decimal? Area { get; set; }

    public string? WikiDataId { get; set; }
    public DefaultIdType? TypeId { get; set; }
    public DefaultIdType StateId { get; set; }
}

public class CreateProvinceRequestHandler : IRequestHandler<CreateProvinceRequest, DefaultIdType>
{
    private readonly IRepository<Province> _repository;
    private readonly IFileStorageService _file;

    public CreateProvinceRequestHandler(IRepository<Province> repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    public async Task<DefaultIdType> Handle(CreateProvinceRequest request, CancellationToken cancellationToken)
    {

        var entity = new Province(
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
            request.Metropolis,
            request.ZipCode,
            request.PhoneCode,
            request.Population,
            request.Area,
            request.WikiDataId,
            request.TypeId,
            request.StateId);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));
        await _repository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }
}
