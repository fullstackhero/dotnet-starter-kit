using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States;

public class UpdateStateRequest : IRequest<DefaultIdType>
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
    public int? NumericCode { get; set; }

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Metropolis { get; set; }
    public DefaultIdType TypeId { get; set; }
    public DefaultIdType CountryId { get; set; }
}

public class UpdateStateRequestHandler : IRequestHandler<UpdateStateRequest, DefaultIdType>
{
    private readonly IStringLocalizer _t;
    private readonly IRepository<State> _repository;
    private readonly IFileStorageService _file;

    public UpdateStateRequestHandler(IRepository<State> repository, IStringLocalizer<UpdateStateRequestHandler> localizer, IFileStorageService file) =>
       (_repository, _t, _file) = (repository, localizer, file);

    public async Task<DefaultIdType> Handle(UpdateStateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);

        entity.Update(
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
            request.TypeId,
            request.CountryId);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.UpdateAsync(entity, cancellationToken);

        return request.Id;
    }
}