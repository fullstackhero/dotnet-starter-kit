using FSH.WebApi.Application.Catalog.GameTypes;

namespace FSH.WebApi.Host.Controllers.Catalog;

public class GameTypesController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.GameTypes)]
    [OpenApiOperation("Search gameTypes using available filters.", "")]
    public Task<PaginationResponse<GameTypeDto>> SearchAsync(SearchGameTypesRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.GameTypes)]
    [OpenApiOperation("Get gameType details.", "")]
    public Task<GameTypeDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetGameTypeRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.GameTypes)]
    [OpenApiOperation("Create a new gameType.", "")]
    public Task<Guid> CreateAsync(CreateGameTypeRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.GameTypes)]
    [OpenApiOperation("Update a gameType.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateGameTypeRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.GameTypes)]
    [OpenApiOperation("Delete a gameType.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteGameTypeRequest(id));
    }

    

    
}