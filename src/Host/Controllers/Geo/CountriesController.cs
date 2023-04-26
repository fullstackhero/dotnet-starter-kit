using FSH.WebApi.Application.Geo.Countries;

namespace FSH.WebApi.Host.Controllers.Geo;

public class CountriesController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.Countries)]
    [OpenApiOperation("Search Countries using available filters.", "")]
    public async Task<PaginationResponse<CountryDto>> SearchAsync(SearchCountriesRequest request)
    {
        return await Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.Countries)]
    [OpenApiOperation("Get Country details.", "")]
    public Task<CountryDetailsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetCountryRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Countries)]
    [OpenApiOperation("Create a new Country.", "")]
    public Task<Guid> CreateAsync(CreateCountryRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.Countries)]
    [OpenApiOperation("Update a Country.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateCountryRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.Countries)]
    [OpenApiOperation("Delete a Country.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteCountryRequest(id));
    }

    [HttpPost("export")]
    [MustHavePermission(FSHAction.Export, FSHResource.Countries)]
    [OpenApiOperation("Export a Countries.", "")]
    public async Task<FileResult> ExportAsync(ExportCountriesRequest filter)
    {
        var result = await Mediator.Send(filter);
        return File(result, "application/octet-stream", "CategoryExports");
    }

    [HttpPost("import")]
    [MustHavePermission(FSHAction.Import, FSHResource.Countries)]
    [OpenApiOperation("Import a Countries.", "")]
    public async Task<ActionResult<int>> ImportAsync(ImportCountriesRequest request)
    {
        return Ok(await Mediator.Send(request));
    }

}