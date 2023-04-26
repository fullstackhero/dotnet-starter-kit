using FSH.WebApi.Application.Geo.States;
using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class DeleteCountryRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }

    public DeleteCountryRequest(DefaultIdType id) => Id = id;
}

public class DeleteCountryRequestHandler : IRequestHandler<DeleteCountryRequest, DefaultIdType>
{
    private readonly IStringLocalizer _t;
    private readonly IRepository<Country> _repository;
    private readonly IReadRepository<State> _childRepository;

    public DeleteCountryRequestHandler(IRepository<Country> repository, IReadRepository<State> childRepository, IStringLocalizer<DeleteCountryRequestHandler> localizer)
            => (_repository, _childRepository, _t) = (repository, childRepository, localizer);

    public async Task<DefaultIdType> Handle(DeleteCountryRequest request, CancellationToken cancellationToken)
    {
        if (await _childRepository.AnyAsync(new StatesByCountrySpec(request.Id), cancellationToken))
        {
            throw new ConflictException(_t["Country cannot be deleted as it's being used."]);
        }

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity ?? throw new NotFoundException(_t["Country {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.DeleteAsync(entity, cancellationToken);

        return request.Id;
    }
}
