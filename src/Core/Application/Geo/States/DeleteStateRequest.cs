using FSH.WebApi.Application.Geo.Provinces;
using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States;

public class DeleteStateRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }

    public DeleteStateRequest(DefaultIdType id) => Id = id;
}

public class DeleteStateRequestHandler : IRequestHandler<DeleteStateRequest, DefaultIdType>
{
    private readonly IStringLocalizer _t;
    private readonly IRepository<State> _repository;
    private readonly IReadRepository<Province> _childRepository;

    public DeleteStateRequestHandler(IRepository<State> repository, IReadRepository<Province> childRepository, IStringLocalizer<DeleteStateRequestHandler> localizer)
            => (_repository, _childRepository, _t) = (repository, childRepository, localizer);

    public async Task<DefaultIdType> Handle(DeleteStateRequest request, CancellationToken cancellationToken)
    {
        if (await _childRepository.AnyAsync(new ProvincesByStateSpec(request.Id), cancellationToken))
        {
            throw new ConflictException(_t["State cannot be deleted as it's being used."]);
        }

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity ?? throw new NotFoundException(_t["State {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.DeleteAsync(entity, cancellationToken);

        return request.Id;
    }
}
