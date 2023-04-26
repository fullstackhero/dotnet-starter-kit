using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces;

public class GetProvinceRequest : IRequest<ProvinceDetailsDto>
{
    public DefaultIdType Id { get; set; }
    public GetProvinceRequest(DefaultIdType id) => Id = id;
}

public class GetProvinceRequestHandler : IRequestHandler<GetProvinceRequest, ProvinceDetailsDto>
{
    private readonly IRepository<Province> _repository;
    private readonly IStringLocalizer _t;

    public GetProvinceRequestHandler(IRepository<Province> repository, IStringLocalizer<GetProvinceRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<ProvinceDetailsDto> Handle(GetProvinceRequest request, CancellationToken cancellationToken) =>
        await _repository.FirstOrDefaultAsync(
            (ISpecification<Province, ProvinceDetailsDto>)new ProvinceByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);
}