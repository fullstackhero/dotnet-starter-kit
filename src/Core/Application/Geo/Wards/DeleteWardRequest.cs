using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards;

public class DeleteWardRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }
    public DeleteWardRequest(DefaultIdType id) => Id = id;
}

public class DeleteWardRequestHandler : IRequestHandler<DeleteWardRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepository<Ward> _repository;
    private readonly IStringLocalizer _t;
 
    public DeleteWardRequestHandler(IRepository<Ward> repository, IStringLocalizer<DeleteWardRequestHandler> localizer)
            => (_repository, _t) = (repository, localizer);

    public async Task<DefaultIdType> Handle(DeleteWardRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity ?? throw new NotFoundException(_t["Ward {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.DeleteAsync(entity, cancellationToken);

        return request.Id;
    }
}
