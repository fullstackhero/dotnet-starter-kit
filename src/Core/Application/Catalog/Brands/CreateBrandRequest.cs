using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Brands;

public class CreateBrandRequest : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class CreateBrandRequestValidator : CustomValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator(IReadRepository<Brand> repository, IStringLocalizer<CreateBrandRequestValidator> localizer) =>
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new BrandByNameSpec(name), ct) is null)
                .WithMessage((_, name) => string.Format(localizer["brand.alreadyexists"], name));
}

public class CreateBrandRequestHandler : IRequestHandler<CreateBrandRequest, Guid>
{
    private readonly IRepository<Brand> _repository;

    public CreateBrandRequestHandler(IRepository<Brand> repository) => _repository = repository;

    public async Task<Guid> Handle(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = new Brand(request.Name, request.Description);

        brand.DomainEvents.Add(new EntityCreatedEvent<Brand>(brand));

        await _repository.AddAsync(brand, cancellationToken);

        return brand.Id;
    }
}