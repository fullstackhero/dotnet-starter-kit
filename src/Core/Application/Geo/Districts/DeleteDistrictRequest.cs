using FSH.WebApi.Application.Geo.Wards;
using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts;

public class DeleteDistrictRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }

    public DeleteDistrictRequest(DefaultIdType id) => Id = id;
}

public class DeleteDistrictRequestHandler : IRequestHandler<DeleteDistrictRequest, DefaultIdType>
{
    private readonly IStringLocalizer _t;
    private readonly IRepository<District> _repository;
    private readonly IReadRepository<Ward> _childRepository;

    public DeleteDistrictRequestHandler(IRepository<District> repository, IReadRepository<Ward> childRepository, IStringLocalizer<DeleteDistrictRequestHandler> localizer)
            => (_repository, _childRepository, _t) = (repository, childRepository, localizer);

    public async Task<DefaultIdType> Handle(DeleteDistrictRequest request, CancellationToken cancellationToken)
    {
        if (await _childRepository.AnyAsync(new WardsByDistrictSpec(request.Id), cancellationToken))
        {
            throw new ConflictException(_t["District cannot be deleted as it's being used."]);
        }

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity ?? throw new NotFoundException(_t["District {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.DeleteAsync(entity, cancellationToken);

        return request.Id;
    }
}
