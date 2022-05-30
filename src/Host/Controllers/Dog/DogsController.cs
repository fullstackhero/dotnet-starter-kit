using FSH.WebApi.Application.Dogs;

namespace FSH.WebApi.Host.Controllers.Dog;

public class DogsController : VersionedApiController
{
    [HttpGet("{id:guid}")]
    [MustHavePermission(FSHAction.View, FSHResource.Brands)]
    [OpenApiOperation("Get dog by Id.", "")]
    public Task<DogDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetDogRequest(id));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Brands)]
    [OpenApiOperation("Create a new dog.", "")]
    public Task<Guid> CreateAsync(CreateDogRequest request)
    {
        return Mediator.Send(request);
    }
}
