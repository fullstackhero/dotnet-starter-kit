using FSH.WebApi.Application.Geo.States;

namespace FSH.WebApi.Host.Controllers.Geo;

public class StatesController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.States)]
    [OpenApiOperation("Search States using available filters.", "")]
    public Task<PaginationResponse<StateDto>> SearchAsync(SearchStatesRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.States)]
    [OpenApiOperation("Get State details.", "")]
    public Task<StateDetailsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetStateRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.States)]
    [OpenApiOperation("Create a new State.", "")]
    public Task<Guid> CreateAsync(CreateStateRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.States)]
    [OpenApiOperation("Update a State.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateStateRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.States)]
    [OpenApiOperation("Delete a State.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteStateRequest(id));
    }

    [HttpPost("export")]
    [MustHavePermission(FSHAction.Export, FSHResource.States)]
    [OpenApiOperation("Export a States.", "")]
    public async Task<FileResult> ExportAsync(ExportStatesRequest filter)
    {
        var result = await Mediator.Send(filter);
        return File(result, "application/octet-stream", "StateExports");
    }

    [HttpPost("import")]
    [MustHavePermission(FSHAction.Import, FSHResource.States)]
    [OpenApiOperation("Import a States.", "")]
    public async Task<ActionResult<int>> ImportAsync(ImportStatesRequest request)
    {
        return Ok(await Mediator.Send(request));
    }

}