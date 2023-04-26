using FSH.WebApi.Application.Geo.GeoAdminUnits;

namespace FSH.WebApi.Host.Controllers.Geo;

public class GeoAdminUnitsController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.GeoAdminUnits)]
    [OpenApiOperation("Search GeoAdminUnits using available filters.", "")]
    public Task<PaginationResponse<GeoAdminUnitDto>> SearchAsync(SearchGeoAdminUnitsRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.GeoAdminUnits)]
    [OpenApiOperation("Get GeoAdminUnit details.", "")]
    public Task<GeoAdminUnitDetailsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetGeoAdminUnitRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.GeoAdminUnits)]
    [OpenApiOperation("Create a new GeoAdminUnit.", "")]
    public Task<Guid> CreateAsync(CreateGeoAdminUnitRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.GeoAdminUnits)]
    [OpenApiOperation("Update a GeoAdminUnit.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateGeoAdminUnitRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.GeoAdminUnits)]
    [OpenApiOperation("Delete a GeoAdminUnit.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteGeoAdminUnitRequest(id));
    }

    [HttpPost("export")]
    [MustHavePermission(FSHAction.Export, FSHResource.GeoAdminUnits)]
    [OpenApiOperation("Export a GeoAdminUnits.", "")]
    public async Task<FileResult> ExportAsync(ExportGeoAdminUnitsRequest filter)
    {
        var result = await Mediator.Send(filter);
        return File(result, "application/octet-stream", "CategoryExports");
    }

    [HttpPost("import")]
    [MustHavePermission(FSHAction.Import, FSHResource.GeoAdminUnits)]
    [OpenApiOperation("Import a GeoAdminUnits.", "")]
    public async Task<ActionResult<int>> ImportAsync(ImportGeoAdminUnitsRequest request)
    {
        return Ok(await Mediator.Send(request));
    }

}