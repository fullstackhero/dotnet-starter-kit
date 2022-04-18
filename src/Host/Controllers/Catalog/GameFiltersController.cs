using FSH.WebApi.Application.Catalog.Filters;

namespace FSH.WebApi.Host.Controllers.Catalog;

public class FiltersController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(FSHAction.Search, FSHResource.Filters)]
    [OpenApiOperation("Search Filters using available filters.", "")]
    public Task<PaginationResponse<FilterDto>> SearchAsync(SearchFiltersRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.Filters)]
    [OpenApiOperation("Get Filter details.", "")]
    public Task<FilterDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetFilterRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Filters)]
    [OpenApiOperation("Create a new Filter.", "")]
    public Task<Guid> CreateAsync(CreateFilterRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.Filters)]
    [OpenApiOperation("Update a Filter.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateFilterRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.Filters)]
    [OpenApiOperation("Delete a Filter.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteFilterRequest(id));
    }

    

    
}