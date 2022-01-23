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
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Brand> _repository;

    public CreateBrandRequestHandler(IRepositoryWithEvents<Brand> repository) => _repository = repository;

    public async Task<Guid> Handle(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = new Brand(request.Name, request.Description);

        await _repository.AddAsync(brand, cancellationToken);

        return brand.Id;
    }
}