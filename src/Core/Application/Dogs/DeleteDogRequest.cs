using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Dogs;
public class DeleteDogRequest : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteDogRequest(Guid id) => Id = id;
}

public class DeleteDogRequestHandler : IRequestHandler<DeleteDogRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Dog> _dogRepo;
    private readonly IStringLocalizer _t;

    public DeleteDogRequestHandler(IRepositoryWithEvents<Dog> dogRepo, IStringLocalizer<DeleteDogRequestHandler> localizer) =>
        (_dogRepo, _t) = (dogRepo, localizer);

    public async Task<Guid> Handle(DeleteDogRequest request, CancellationToken cancellationToken)
    {
        var dog = await _dogRepo.GetByIdAsync(request.Id, cancellationToken);

        _ = dog ?? throw new NotFoundException(_t[$"Dog {request.Id} Not found."]);

        await _dogRepo.DeleteAsync(dog, cancellationToken);

        return request.Id;
    }
}
