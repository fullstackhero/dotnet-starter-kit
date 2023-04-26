using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts;

public class GetDistrictRequest : IRequest<DistrictDetailsDto>
{
    public DefaultIdType Id { get; set; }
    public GetDistrictRequest(DefaultIdType id) => Id = id;
}

public class GetDistrictRequestHandler : IRequestHandler<GetDistrictRequest, DistrictDetailsDto>
{
    private readonly IRepository<District> _repository;
    private readonly IStringLocalizer _t;

    public GetDistrictRequestHandler(IRepository<District> repository, IStringLocalizer<GetDistrictRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<DistrictDetailsDto> Handle(GetDistrictRequest request, CancellationToken cancellationToken) =>
        await _repository.FirstOrDefaultAsync(
            (ISpecification<District, DistrictDetailsDto>)new DistrictByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);
}