using FSH.WebApi.Application.Dogs;

namespace FSH.WebApi.Host.Controllers.Dog;

public class DogsController : VersionedApiController
{
    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.Dogs)]
    [OpenApiOperation("Get dog by Id.", "")]
    public Task<DogDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetDogRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Dogs)]
    [OpenApiOperation("Create a new dog.", "")]
    public Task<Guid> CreateAsync(CreateDogRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(FSHAction.Update, FSHResource.Dogs)]
    [OpenApiOperation("Update a Dog.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateDogRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(FSHAction.Delete, FSHResource.Dogs)]
    [OpenApiOperation("Delete a dog.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteDogRequest(id));
    }
}
