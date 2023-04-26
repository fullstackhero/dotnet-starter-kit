using FSH.WebApi.Application.Geo.Districts;

namespace FSH.WebApi.Host.Controllers.Geo;

public class DistrictsController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.Districts)]
    [OpenApiOperation("Search Districts using available filters.", "")]
    public Task<PaginationResponse<DistrictDto>> SearchAsync(SearchDistrictsRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.Districts)]
    [OpenApiOperation("Get District details.", "")]
    public Task<DistrictDetailsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetDistrictRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Districts)]
    [OpenApiOperation("Create a new District.", "")]
    public Task<Guid> CreateAsync(CreateDistrictRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.Districts)]
    [OpenApiOperation("Update a District.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateDistrictRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.Districts)]
    [OpenApiOperation("Delete a District.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteDistrictRequest(id));
    }

    [HttpPost("export")]
    [MustHavePermission(FSHAction.Export, FSHResource.Districts)]
    [OpenApiOperation("Export a Districts.", "")]
    public async Task<FileResult> ExportAsync(ExportDistrictsRequest filter)
    {
        var result = await Mediator.Send(filter);
        return File(result, "application/octet-stream", "DistrictExports");
    }

    [HttpPost("import")]
    [MustHavePermission(FSHAction.Import, FSHResource.Districts)]
    [OpenApiOperation("Import a Districts.", "")]
    public async Task<ActionResult<int>> ImportAsync(ImportDistrictsRequest request)
    {
        return Ok(await Mediator.Send(request));
    }

}