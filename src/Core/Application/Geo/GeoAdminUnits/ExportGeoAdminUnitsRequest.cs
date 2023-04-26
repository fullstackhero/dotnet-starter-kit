using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;

public class ExportGeoAdminUnitsRequest : BaseFilter, IRequest<Stream>
{
}

public class ExportGeoAdminUnitsRequestHandler : IRequestHandler<ExportGeoAdminUnitsRequest, Stream>
{
    private readonly IReadRepository<GeoAdminUnit> _repository;
    private readonly IExcelWriter _excelWriter;

    public ExportGeoAdminUnitsRequestHandler(IReadRepository<GeoAdminUnit> repository, IExcelWriter excelWriter)
    {
        _repository = repository;
        _excelWriter = excelWriter;
    }

    public async Task<Stream> Handle(ExportGeoAdminUnitsRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportGeoAdminUnitsSpecification(request);

        var list = await _repository.ListAsync(spec, cancellationToken);

        return _excelWriter.WriteToStream(list);
    }
}

public class ExportGeoAdminUnitsSpecification : EntitiesByBaseFilterSpec<GeoAdminUnit, GeoAdminUnitExportDto>
{
    public ExportGeoAdminUnitsSpecification(ExportGeoAdminUnitsRequest request)
        : base(request) =>
        Query
           .SearchBy(request);
}