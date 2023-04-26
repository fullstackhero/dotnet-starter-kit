using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces;

public class ExportProvincesRequest : BaseFilter, IRequest<Stream>
{
}

public class ExportProvincesRequestHandler : IRequestHandler<ExportProvincesRequest, Stream>
{
    private readonly IReadRepository<Province> _repository;
    private readonly IExcelWriter _excelWriter;

    public ExportProvincesRequestHandler(IReadRepository<Province> repository, IExcelWriter excelWriter)
    {
        _repository = repository;
        _excelWriter = excelWriter;
    }

    public async Task<Stream> Handle(ExportProvincesRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportProvincesSpecification(request);

        var list = await _repository.ListAsync(spec, cancellationToken);

        return _excelWriter.WriteToStream(list);
    }
}

public class ExportProvincesSpecification : EntitiesByBaseFilterSpec<Province, ProvinceExportDto>
{
    public ExportProvincesSpecification(ExportProvincesRequest request)
        : base(request) =>
        Query
           .SearchBy(request);
}