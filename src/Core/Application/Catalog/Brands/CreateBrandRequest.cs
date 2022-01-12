namespace FSH.WebApi.Application.Catalog.Brands;

public class CreateBrandRequest : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class CreateBrandRequestHandler : IRequestHandler<CreateBrandRequest, Guid>
{
    private readonly IRepository<Brand> _repository;

    public CreateBrandRequestHandler(IRepository<Brand> repository) => _repository = repository;

    public async Task<Guid> Handle(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = new Brand(request.Name, request.Description);

        brand.DomainEvents.Add(new BrandCreatedEvent(brand));

        await _repository.AddAsync(brand, cancellationToken);

        return brand.Id;
    }
}