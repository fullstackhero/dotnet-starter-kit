using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States;

public class GetStateRequest : IRequest<StateDetailsDto>
{
    public DefaultIdType Id { get; set; }
    public GetStateRequest(DefaultIdType id) => Id = id;
}

public class GetStateRequestHandler : IRequestHandler<GetStateRequest, StateDetailsDto>
{
    private readonly IRepository<State> _repository;
    private readonly IStringLocalizer _t;

    public GetStateRequestHandler(IRepository<State> repository, IStringLocalizer<GetStateRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<StateDetailsDto> Handle(GetStateRequest request, CancellationToken cancellationToken) =>
        await _repository.FirstOrDefaultAsync(
            (ISpecification<State, StateDetailsDto>)new StateByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);
}