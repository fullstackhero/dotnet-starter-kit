using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameFilters;

public class UpdateGameFilterRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Rules { get; set; }
}

public class UpdateGameFilterRequestValidator : CustomValidator<UpdateGameFilterRequest>
{
    public UpdateGameFilterRequestValidator(IRepository<GameFilter> repository, IStringLocalizer<UpdateGameFilterRequestValidator> T) =>
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (GameFilter, name, ct) =>
                    await repository.GetBySpecAsync(new GameFilterByNameSpec(name), ct)
                        is not GameFilter existingGameFilter || existingGameFilter.Id == GameFilter.Id)
                .WithMessage((_, name) => T["GameFilter {0} already Exists.", name]);
}

public class UpdateGameFilterRequestHandler : IRequestHandler<UpdateGameFilterRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<GameFilter> _repository;
    private readonly IStringLocalizer _t;

    public UpdateGameFilterRequestHandler(IRepositoryWithEvents<GameFilter> repository, IStringLocalizer<UpdateGameFilterRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(UpdateGameFilterRequest request, CancellationToken cancellationToken)
    {
        var GameFilter = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = GameFilter
        ?? throw new NotFoundException(_t["GameFilter {0} Not Found.", request.Id]);

        GameFilter.Update(request.Name, request.Description,request.Rules);
        // Add Domain Events to be raised after the commit
        GameFilter.DomainEvents.Add(EntityUpdatedEvent.WithEntity(GameFilter));
        await _repository.UpdateAsync(GameFilter, cancellationToken);

        return request.Id;
    }
}