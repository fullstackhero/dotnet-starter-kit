using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards;

public class GetWardRequest : IRequest<WardDetailsDto>
{
    public DefaultIdType Id { get; set; }
    public GetWardRequest(DefaultIdType id) => Id = id;
}

public class GetWardRequestHandler : IRequestHandler<GetWardRequest, WardDetailsDto>
{
    private readonly IRepository<Ward> _repository;
    private readonly IStringLocalizer _t;

    public GetWardRequestHandler(IRepository<Ward> repository, IStringLocalizer<GetWardRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<WardDetailsDto> Handle(GetWardRequest request, CancellationToken cancellationToken) =>
        await _repository.FirstOrDefaultAsync(
            (ISpecification<Ward, WardDetailsDto>)new WardByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);
}