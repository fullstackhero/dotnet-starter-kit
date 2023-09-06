using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.Filters;
public class CreateFilterRequest: IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string Color { get; set; }
    
}
public class CreateFilterRequestValidator: CustomValidator<CreateFilterRequest>
{
    public CreateFilterRequestValidator(IReadRepository<Filter> repository, IStringLocalizer<CreateFilterRequestValidator> T) =>
    RuleFor(p => p.Name)
        .NotEmpty()
        .MaximumLength(100)
        .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new FilterByNameSpec(name), ct) is null)
        .WithMessage((_, name) => T["Game type {0} already exists", name]);
}
public class CreateFilterRequestHandler : IRequestHandler<CreateFilterRequest, Guid>
{
    private readonly IRepositoryWithEvents<Filter> _repository;

    public CreateFilterRequestHandler(IRepositoryWithEvents<Filter> repository) => _repository = repository;
    
    public async Task<Guid> Handle(CreateFilterRequest request, CancellationToken cancellationToken)
    {
        var Filter = new Filter(request.Name, request.Description, request.Rules);
        Filter.DomainEvents.Add(EntityCreatedEvent.WithEntity(Filter));
        await _repository.AddAsync(Filter);
        return Filter.Id;
    }
}

