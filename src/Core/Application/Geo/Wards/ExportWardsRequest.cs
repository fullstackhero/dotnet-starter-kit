using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards;

public class ExportWardsRequest : BaseFilter, IRequest<Stream>
{
}

public class ExportWardsRequestHandler : IRequestHandler<ExportWardsRequest, Stream>
{
    private readonly IReadRepository<Ward> _repository;
    private readonly IExcelWriter _excelWriter;

    public ExportWardsRequestHandler(IReadRepository<Ward> repository, IExcelWriter excelWriter)
    {
        _repository = repository;
        _excelWriter = excelWriter;
    }

    public async Task<Stream> Handle(ExportWardsRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportWardsSpecification(request);

        var list = await _repository.ListAsync(spec, cancellationToken);

        return _excelWriter.WriteToStream(list);
    }
}

public class ExportWardsSpecification : EntitiesByBaseFilterSpec<Ward, WardExportDto>
{
    public ExportWardsSpecification(ExportWardsRequest request)
        : base(request) =>
        Query
           .SearchBy(request);
}