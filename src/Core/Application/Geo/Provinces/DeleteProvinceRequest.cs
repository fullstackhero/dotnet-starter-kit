using FSH.WebApi.Application.Geo.Districts;
using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces;

public class DeleteProvinceRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }

    public DeleteProvinceRequest(DefaultIdType id) => Id = id;
}

public class DeleteProvinceRequestHandler : IRequestHandler<DeleteProvinceRequest, DefaultIdType>
{
    private readonly IStringLocalizer _t;
    private readonly IRepository<Province> _repository;
    private readonly IReadRepository<District> _childRepository;

    public DeleteProvinceRequestHandler(IRepository<Province> repository, IReadRepository<District> childRepository, IStringLocalizer<DeleteProvinceRequestHandler> localizer)
            => (_repository, _childRepository, _t) = (repository, childRepository, localizer);

    public async Task<DefaultIdType> Handle(DeleteProvinceRequest request, CancellationToken cancellationToken)
    {
        if (await _childRepository.AnyAsync(new DistrictsByProvinceSpec(request.Id), cancellationToken))
        {
            throw new ConflictException(_t["Province cannot be deleted as it's being used."]);
        }

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity ?? throw new NotFoundException(_t["Province {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.DeleteAsync(entity, cancellationToken);

        return request.Id;
    }
}
