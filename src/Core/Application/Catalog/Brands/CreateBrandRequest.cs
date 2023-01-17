using FSH.WebApi.Application.Common.Persistence;

namespace FSH.WebApi.Application.Catalog.Brands;

public class CreateBrandRequest : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class CreateBrandRequestValidator : CustomValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator(IReadRepository<Brand> repository, IStringLocalizer<CreateBrandRequestValidator> T) =>
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new BrandByNameSpec(name), ct) is null)
                .WithMessage((_, name) => T["Brand {0} already Exists.", name]);
}

public class CreateBrandRequestHandler : IRequestHandler<CreateBrandRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Brand> _repository;
    private readonly IReadRepository<Brand> _readRepository;
    private readonly IStringLocalizer<CreateBrandRequestValidator> _t;

    public CreateBrandRequestHandler(IRepositoryWithEvents<Brand> repository, IReadRepository<Brand> readRepository, IStringLocalizer<CreateBrandRequestValidator> t)
    {
        _readRepository = readRepository;
        _repository = repository;
        _t = t;
    }

    public async Task<Guid> Handle(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var validation = new CreateBrandRequestValidator(_readRepository, _t);
        await validation.ValidateAndThrowAsync(request, cancellationToken);

        var brand = new Brand(request.Name, request.Description);

        await _repository.AddAsync(brand, cancellationToken);

        return brand.Id;
    }
}