using FSH.WebApi.Application.Catalog.Products;

namespace FSH.WebApi.Application.Catalog.Brands;

public class DeleteBrandRequest : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteBrandRequest(Guid id) => Id = id;
}

public class DeleteBrandRequestHandler : IRequestHandler<DeleteBrandRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Brand> _brandRepo;
    private readonly IReadRepository<Product> _productRepo;
    private readonly IStringLocalizer _t;

    public DeleteBrandRequestHandler(IRepositoryWithEvents<Brand> brandRepo, IReadRepository<Product> productRepo, IStringLocalizer<DeleteBrandRequestHandler> localizer) =>
        (_brandRepo, _productRepo, _t) = (brandRepo, productRepo, localizer);

    public async Task<Guid> Handle(DeleteBrandRequest request, CancellationToken cancellationToken)
    {
        if (await _productRepo.AnyAsync(new ProductsByBrandSpec(request.Id), cancellationToken))
        {
            throw new ConflictException(_t["Brand cannot be deleted as it's being used."]);
        }

        var brand = await _brandRepo.GetByIdAsync(request.Id, cancellationToken);

        _ = brand ?? throw new NotFoundException(_t["Brand {0} Not Found."]);

        await _brandRepo.DeleteAsync(brand, cancellationToken);

        return request.Id;
    }
}