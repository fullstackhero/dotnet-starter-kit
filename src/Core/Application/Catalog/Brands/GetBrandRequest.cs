namespace FSH.WebApi.Application.Catalog.Brands;

public class GetBrandRequest : IRequest<BrandDto>
{
    public Guid Id { get; set; }

    public GetBrandRequest(in Guid id) => Id = id;
}

public class GetBrandRequestHandler : IRequestHandler<GetBrandRequest, BrandDto>
{
    private readonly IRepository<Brand> _repository;
    private readonly IStringLocalizer<GetBrandRequestHandler> _localizer;

    public GetBrandRequestHandler(IRepository<Brand> repository, IStringLocalizer<GetBrandRequestHandler> localizer) => (_repository, _localizer) = (repository, localizer);

    public Task<BrandDto> Handle(GetBrandRequest request, CancellationToken cancellationToken) =>
        _repository.GetBySpecAsync(
            (ISpecification<Brand, BrandDto>)new BrandByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(string.Format(_localizer["brand.notfound"], request.Id));
}