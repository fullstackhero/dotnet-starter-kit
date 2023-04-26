using FSH.WebApi.Application.Geo.Wards;

namespace FSH.WebApi.Host.Controllers.Geo;

public class WardsController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.Wards)]
    [OpenApiOperation("Search Wards using available filters.", "")]
    public Task<PaginationResponse<WardDto>> SearchAsync(SearchWardsRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.Wards)]
    [OpenApiOperation("Get Ward details.", "")]
    public Task<WardDetailsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetWardRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Wards)]
    [OpenApiOperation("Create a new Ward.", "")]
    public Task<Guid> CreateAsync(CreateWardRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.Wards)]
    [OpenApiOperation("Update a Ward.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateWardRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.Wards)]
    [OpenApiOperation("Delete a Ward.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteWardRequest(id));
    }

    [HttpPost("export")]
    [MustHavePermission(FSHAction.Export, FSHResource.Wards)]
    [OpenApiOperation("Export a Wards.", "")]
    public async Task<FileResult> ExportAsync(ExportWardsRequest filter)
    {
        var result = await Mediator.Send(filter);
        return File(result, "application/octet-stream", "WardExports");
    }

    [HttpPost("import")]
    [MustHavePermission(FSHAction.Import, FSHResource.Wards)]
    [OpenApiOperation("Import a Wards.", "")]
    public async Task<ActionResult<int>> ImportAsync(ImportWardsRequest request)
    {
        return Ok(await Mediator.Send(request));
    }

}