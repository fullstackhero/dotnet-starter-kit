using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States;

public class ExportStatesRequest : BaseFilter, IRequest<Stream>
{
}

public class ExportStatesRequestHandler : IRequestHandler<ExportStatesRequest, Stream>
{
    private readonly IReadRepository<State> _repository;
    private readonly IExcelWriter _excelWriter;

    public ExportStatesRequestHandler(IReadRepository<State> repository, IExcelWriter excelWriter)
    {
        _repository = repository;
        _excelWriter = excelWriter;
    }

    public async Task<Stream> Handle(ExportStatesRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportStatesSpecification(request);

        var list = await _repository.ListAsync(spec, cancellationToken);

        return _excelWriter.WriteToStream(list);
    }
}

public class ExportStatesSpecification : EntitiesByBaseFilterSpec<State, StateExportDto>
{
    public ExportStatesSpecification(ExportStatesRequest request)
        : base(request) =>
        Query
           .SearchBy(request);
}