using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts;

public class ExportDistrictsRequest : BaseFilter, IRequest<Stream>
{
}

public class ExportDistrictsRequestHandler : IRequestHandler<ExportDistrictsRequest, Stream>
{
    private readonly IReadRepository<District> _repository;
    private readonly IExcelWriter _excelWriter;

    public ExportDistrictsRequestHandler(IReadRepository<District> repository, IExcelWriter excelWriter)
    {
        _repository = repository;
        _excelWriter = excelWriter;
    }

    public async Task<Stream> Handle(ExportDistrictsRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportDistrictsSpecification(request);

        var list = await _repository.ListAsync(spec, cancellationToken);

        return _excelWriter.WriteToStream(list);
    }
}

public class ExportDistrictsSpecification : EntitiesByBaseFilterSpec<District, DistrictExportDto>
{
    public ExportDistrictsSpecification(ExportDistrictsRequest request)
        : base(request) =>
        Query
           .SearchBy(request);
}