using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Filters;

public class UpdateFilterRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Rules { get; set; }
}

public class UpdateFilterRequestValidator : CustomValidator<UpdateFilterRequest>
{
    public UpdateFilterRequestValidator(IRepository<Filter> repository, IStringLocalizer<UpdateFilterRequestValidator> T) =>
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (Filter, name, ct) =>
                    await repository.GetBySpecAsync(new FilterByNameSpec(name), ct)
                        is not Filter existingFilter || existingFilter.Id == Filter.Id)
                .WithMessage((_, name) => T["Filter {0} already Exists.", name]);
}

public class UpdateFilterRequestHandler : IRequestHandler<UpdateFilterRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Filter> _repository;
    private readonly IStringLocalizer _t;

    public UpdateFilterRequestHandler(IRepositoryWithEvents<Filter> repository, IStringLocalizer<UpdateFilterRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(UpdateFilterRequest request, CancellationToken cancellationToken)
    {
        var Filter = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = Filter
        ?? throw new NotFoundException(_t["Filter {0} Not Found.", request.Id]);

        Filter.Update(request.Name, request.Description,request.Rules);
        // Add Domain Events to be raised after the commit
        Filter.DomainEvents.Add(EntityUpdatedEvent.WithEntity(Filter));
        await _repository.UpdateAsync(Filter, cancellationToken);

        return request.Id;
    }
}