using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class ExportCountriesRequest : BaseFilter, IRequest<Stream>
{
}

public class ExportCountriesRequestHandler : IRequestHandler<ExportCountriesRequest, Stream>
{
    private readonly IReadRepository<Country> _repository;
    private readonly IExcelWriter _excelWriter;

    public ExportCountriesRequestHandler(IReadRepository<Country> repository, IExcelWriter excelWriter)
    {
        _repository = repository;
        _excelWriter = excelWriter;
    }

    public async Task<Stream> Handle(ExportCountriesRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportCountriesSpecification(request);

        var list = await _repository.ListAsync(spec, cancellationToken);

        return _excelWriter.WriteToStream(list);
    }
}

public class ExportCountriesSpecification : EntitiesByBaseFilterSpec<Country, CountryExportDto>
{
    public ExportCountriesSpecification(ExportCountriesRequest request)
        : base(request) =>
        Query
           .SearchBy(request);
}