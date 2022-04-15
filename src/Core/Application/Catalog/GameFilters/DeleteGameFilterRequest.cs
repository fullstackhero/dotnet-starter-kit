using FSH.WebApi.Application.Catalog.Products;
using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameFilters;

public class DeleteGameFilterRequest : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteGameFilterRequest(Guid id) => Id = id;
}

public class DeleteGameFilterRequestHandler : IRequestHandler<DeleteGameFilterRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<GameFilter> _GameFilterRepo;
    
    private readonly IStringLocalizer _t;

    public DeleteGameFilterRequestHandler(IRepositoryWithEvents<GameFilter> GameFilterRepo,   IStringLocalizer<DeleteGameFilterRequestHandler> localizer) =>
        (_GameFilterRepo,  _t) = (GameFilterRepo,   localizer);

    public async Task<Guid> Handle(DeleteGameFilterRequest request, CancellationToken cancellationToken)
    {
        //if (await _productRepo.AnyAsync(new ProductsByBrandSpec(request.Id), cancellationToken))
        //{
        //    throw new ConflictException(_t["Game type cannot be deleted as it's being used."]);
        //}

        var GameFilter = await _GameFilterRepo.GetByIdAsync(request.Id, cancellationToken);

        _ = GameFilter ?? throw new NotFoundException(_t["Game type {0} Not Found."]);
        // Add Domain Events to be raised after the commit
        GameFilter.DomainEvents.Add(EntityDeletedEvent.WithEntity(GameFilter));
        await _GameFilterRepo.DeleteAsync(GameFilter, cancellationToken);

        return request.Id;
    }
}