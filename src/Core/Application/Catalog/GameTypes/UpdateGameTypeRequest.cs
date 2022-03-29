using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameTypes;

public class UpdateGameTypeRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Rules { get; set; }
}

public class UpdateGameTypeRequestValidator : CustomValidator<UpdateGameTypeRequest>
{
    public UpdateGameTypeRequestValidator(IRepository<GameType> repository, IStringLocalizer<UpdateGameTypeRequestValidator> T) =>
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (gameType, name, ct) =>
                    await repository.GetBySpecAsync(new GameTypeByNameSpec(name), ct)
                        is not GameType existingGameType || existingGameType.Id == gameType.Id)
                .WithMessage((_, name) => T["GameType {0} already Exists.", name]);
}

public class UpdateGameTypeRequestHandler : IRequestHandler<UpdateGameTypeRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<GameType> _repository;
    private readonly IStringLocalizer _t;

    public UpdateGameTypeRequestHandler(IRepositoryWithEvents<GameType> repository, IStringLocalizer<UpdateGameTypeRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(UpdateGameTypeRequest request, CancellationToken cancellationToken)
    {
        var gameType = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = gameType
        ?? throw new NotFoundException(_t["GameType {0} Not Found.", request.Id]);

        gameType.Update(request.Name, request.Description,request.Rules);
        // Add Domain Events to be raised after the commit
        gameType.DomainEvents.Add(EntityUpdatedEvent.WithEntity(gameType));
        await _repository.UpdateAsync(gameType, cancellationToken);

        return request.Id;
    }
}