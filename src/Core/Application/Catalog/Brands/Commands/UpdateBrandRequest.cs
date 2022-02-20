namespace FSH.WebApi.Application.Catalog.Brands;

public class UpdateBrandRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class UpdateBrandRequestValidator : CustomValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator(IRepository<Brand> repository, IStringLocalizer<UpdateBrandRequestValidator> localizer) =>
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (brand, name, ct) =>
                    await repository.GetBySpecAsync(new BrandByNameSpec(name), ct)
                        is not Brand existingBrand || existingBrand.Id == brand.Id)
                .WithMessage((_, name) => string.Format(localizer["brand.alreadyexists"], name));
}

public class UpdateBrandRequestHandler : IRequestHandler<UpdateBrandRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Brand> _repository;
    private readonly IStringLocalizer<UpdateBrandRequestHandler> _localizer;

    public UpdateBrandRequestHandler(IRepositoryWithEvents<Brand> repository, IStringLocalizer<UpdateBrandRequestHandler> localizer) =>
        (_repository, _localizer) = (repository, localizer);

    public async Task<Guid> Handle(UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = brand ?? throw new NotFoundException(string.Format(_localizer["brand.notfound"], request.Id));

        brand.Update(request.Name, request.Description);

        await _repository.UpdateAsync(brand, cancellationToken);

        return request.Id;
    }
}