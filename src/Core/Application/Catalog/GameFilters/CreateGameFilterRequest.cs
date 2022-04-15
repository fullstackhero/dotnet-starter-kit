using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.GameFilters;
public class CreateGameFilterRequest: IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string Color { get; set; }
    
}
public class CreateGameFilterRequestValidator: CustomValidator<CreateGameFilterRequest>
{
    public CreateGameFilterRequestValidator(IReadRepository<GameFilter> repository, IStringLocalizer<CreateGameFilterRequestValidator> T) =>
    RuleFor(p => p.Name)
        .NotEmpty()
        .MaximumLength(100)
        .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new GameFilterByNameSpec(name), ct) is null)
        .WithMessage((_, name) => T["Game type {0} already exists", name]);
}
public class CreateGameFilterRequestHandler : IRequestHandler<CreateGameFilterRequest, Guid>
{
    private readonly IRepositoryWithEvents<GameFilter> _repository;

    public CreateGameFilterRequestHandler(IRepositoryWithEvents<GameFilter> repository) => _repository = repository;
    
    public async Task<Guid> Handle(CreateGameFilterRequest request, CancellationToken cancellationToken)
    {
        var GameFilter = new GameFilter(request.Name, request.Description, request.Rules);
        GameFilter.DomainEvents.Add(EntityCreatedEvent.WithEntity(GameFilter));
        await _repository.AddAsync(GameFilter);
        return GameFilter.Id;
    }
}

