using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class ImportCountriesRequest : IRequest<int>
{
    public FileUploadRequest ExcelFile { get; set; } = default!;
}

public class ImportCountriesRequestHandler : IRequestHandler<ImportCountriesRequest, int>
{
    private readonly IDapperRepository _repository;
    private readonly IExcelReader _excelReader;
    private readonly IStringLocalizer _localizer;
    public ImportCountriesRequestHandler(
        IDapperRepository repository,
        IExcelReader excelReader,
        IStringLocalizer<ImportCountriesRequestHandler> localizer)
    {
        _repository = repository;
        _excelReader = excelReader;
        _localizer = localizer;
    }

    public async Task<int> Handle(ImportCountriesRequest request, CancellationToken cancellationToken)
    {
        var items = await _excelReader.ToListAsync<Country>(request.ExcelFile, FileType.Excel);

        if (items?.Count > 0)
        {
            try
            {
                await _repository.UpdateRangeAsync(items, cancellationToken);
            }
            catch (Exception)
            {
                throw new InternalServerException(_localizer["Internal error!"]);
            }
        }

        return items?.Count ?? 0;
    }
}
