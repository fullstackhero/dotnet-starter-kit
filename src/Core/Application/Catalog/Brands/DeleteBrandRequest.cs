using FSH.WebApi.Application.Catalog.Products;

namespace FSH.WebApi.Application.Catalog.Brands;

public class DeleteBrandRequest : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteBrandRequest(Guid id) => Id = id;
}

public class DeleteBrandRequestHandler : IRequestHandler<DeleteBrandRequest, Guid>
{
    private readonly IRepository<Brand> _brandRepo;
    private readonly IReadRepository<Product> _productRepo;
    private readonly IStringLocalizer<DeleteBrandRequestHandler> _localizer;

    public DeleteBrandRequestHandler(IRepository<Brand> brandRepo, IReadRepository<Product> productRepo, IStringLocalizer<DeleteBrandRequestHandler> localizer) =>
        (_brandRepo, _productRepo, _localizer) = (brandRepo, productRepo, localizer);

    public async Task<Guid> Handle(DeleteBrandRequest request, CancellationToken cancellationToken)
    {
        if (await _productRepo.AnyAsync(new ProductsByBrandSpec(request.Id), cancellationToken))
        {
            throw new ConflictException(_localizer["brand.cannotbedeleted"]);
        }

        var brand = await _brandRepo.GetByIdAsync(request.Id);

        _ = brand ?? throw new NotFoundException(_localizer["brand.notfound"]);

        brand.DomainEvents.Add(new BrandDeletedEvent(brand));

        await _brandRepo.DeleteAsync(brand, cancellationToken);

        return request.Id;
    }
}