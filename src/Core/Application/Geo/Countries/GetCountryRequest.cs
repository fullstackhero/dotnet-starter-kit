using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class GetCountryRequest : IRequest<CountryDetailsDto>
{
    public DefaultIdType Id { get; set; }
    public GetCountryRequest(DefaultIdType id) => Id = id;
}

public class GetCountryRequestHandler : IRequestHandler<GetCountryRequest, CountryDetailsDto>
{
    private readonly IRepository<Country> _repository;
    private readonly IStringLocalizer _t;

    public GetCountryRequestHandler(IRepository<Country> repository, IStringLocalizer<GetCountryRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<CountryDetailsDto> Handle(GetCountryRequest request, CancellationToken cancellationToken) =>
        await _repository.FirstOrDefaultAsync(
            (ISpecification<Country, CountryDetailsDto>)new CountryByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);
}