using FSH.WebApi.Application.Catalog.GameFilters;

namespace FSH.WebApi.Host.Controllers.Catalog;

public class GameFiltersController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.GameFilters)]
    [OpenApiOperation("Search GameFilters using available filters.", "")]
    public Task<PaginationResponse<GameFilterDto>> SearchAsync(SearchGameFiltersRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.GameFilters)]
    [OpenApiOperation("Get GameFilter details.", "")]
    public Task<GameFilterDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetGameFilterRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.GameFilters)]
    [OpenApiOperation("Create a new GameFilter.", "")]
    public Task<Guid> CreateAsync(CreateGameFilterRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.GameFilters)]
    [OpenApiOperation("Update a GameFilter.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateGameFilterRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.GameFilters)]
    [OpenApiOperation("Delete a GameFilter.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteGameFilterRequest(id));
    }

    

    
}