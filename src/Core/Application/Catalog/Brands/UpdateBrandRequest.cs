namespace FSH.WebApi.Application.Catalog.Brands;

public class UpdateBrandRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class UpdateBrandRequestHandler : IRequestHandler<UpdateBrandRequest, Guid>
{
    private readonly IRepository<Brand> _repository;
    private readonly IStringLocalizer<UpdateBrandRequestHandler> _localizer;

    public UpdateBrandRequestHandler(IRepository<Brand> repository, IStringLocalizer<UpdateBrandRequestHandler> localizer) =>
        (_repository, _localizer) = (repository, localizer);

    public async Task<Guid> Handle(UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = brand ?? throw new NotFoundException(string.Format(_localizer["brand.notfound"], request.Id));

        brand.Update(request.Name, request.Description);

        brand.DomainEvents.Add(new BrandUpdatedEvent(brand));

        await _repository.UpdateAsync(brand, cancellationToken);

        return request.Id;
    }
}