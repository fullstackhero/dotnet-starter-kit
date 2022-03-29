using FSH.WebApi.Application.Catalog.Products;
using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameTypes;

public class DeleteGameTypeRequest : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteGameTypeRequest(Guid id) => Id = id;
}

public class DeleteGameTypeRequestHandler : IRequestHandler<DeleteGameTypeRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<GameType> _gameTypeRepo;
    
    private readonly IStringLocalizer _t;

    public DeleteGameTypeRequestHandler(IRepositoryWithEvents<GameType> gameTypeRepo,   IStringLocalizer<DeleteGameTypeRequestHandler> localizer) =>
        (_gameTypeRepo,  _t) = (gameTypeRepo,   localizer);

    public async Task<Guid> Handle(DeleteGameTypeRequest request, CancellationToken cancellationToken)
    {
        //if (await _productRepo.AnyAsync(new ProductsByBrandSpec(request.Id), cancellationToken))
        //{
        //    throw new ConflictException(_t["Game type cannot be deleted as it's being used."]);
        //}

        var gameType = await _gameTypeRepo.GetByIdAsync(request.Id, cancellationToken);

        _ = gameType ?? throw new NotFoundException(_t["Game type {0} Not Found."]);
        // Add Domain Events to be raised after the commit
        gameType.DomainEvents.Add(EntityDeletedEvent.WithEntity(gameType));
        await _gameTypeRepo.DeleteAsync(gameType, cancellationToken);

        return request.Id;
    }
}