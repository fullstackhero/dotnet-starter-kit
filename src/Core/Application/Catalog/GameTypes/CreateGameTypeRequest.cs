using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.GameTypes;
public class CreateGameTypeRequest: IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Rules { get; set; }
}
public class CreateGameTypeRequestValidator: CustomValidator<CreateGameTypeRequest>
{
    public CreateGameTypeRequestValidator(IReadRepository<GameType> repository, IStringLocalizer<CreateGameTypeRequestValidator> T) =>
    RuleFor(p => p.Name)
        .NotEmpty()
        .MaximumLength(100)
        .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new GameTypeByNameSpec(name), ct) is null)
        .WithMessage((_, name) => T["Game type {0} already exists", name]);
}
public class CreateGameTypeRequestHandler : IRequestHandler<CreateGameTypeRequest, Guid>
{
    private readonly IRepositoryWithEvents<GameType> _repository;

    public CreateGameTypeRequestHandler(IRepositoryWithEvents<GameType> repository) => _repository = repository;
    
    public async Task<Guid> Handle(CreateGameTypeRequest request, CancellationToken cancellationToken)
    {
        var gameType = new GameType(request.Name, request.Description, request.Rules);
        gameType.DomainEvents.Add(EntityCreatedEvent.WithEntity(gameType));
        await _repository.AddAsync(gameType);
        return gameType.Id;
    }
}

