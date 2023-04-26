using FSH.WebApi.Application.Geo.Provinces;

namespace FSH.WebApi.Host.Controllers.Geo;

public class ProvincesController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.Provinces)]
    [OpenApiOperation("Search Provinces using available filters.", "")]
    public Task<PaginationResponse<ProvinceDto>> SearchAsync(SearchProvincesRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.Provinces)]
    [OpenApiOperation("Get Province details.", "")]
    public Task<ProvinceDetailsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetProvinceRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Provinces)]
    [OpenApiOperation("Create a new Province.", "")]
    public Task<Guid> CreateAsync(CreateProvinceRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.Provinces)]
    [OpenApiOperation("Update a Province.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateProvinceRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.Provinces)]
    [OpenApiOperation("Delete a Province.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteProvinceRequest(id));
    }

    [HttpPost("export")]
    [MustHavePermission(FSHAction.Export, FSHResource.Provinces)]
    [OpenApiOperation("Export a Provinces.", "")]
    public async Task<FileResult> ExportAsync(ExportProvincesRequest filter)
    {
        var result = await Mediator.Send(filter);
        return File(result, "application/octet-stream", "ProvinceExports");
    }

    [HttpPost("import")]
    [MustHavePermission(FSHAction.Import, FSHResource.Provinces)]
    [OpenApiOperation("Import a Provinces.", "")]
    public async Task<ActionResult<int>> ImportAsync(ImportProvincesRequest request)
    {
        return Ok(await Mediator.Send(request));
    }

}