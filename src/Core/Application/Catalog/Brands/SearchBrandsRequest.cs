using DN.WebApi.Application.Common.Models;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Application.Common.Specification;
using DN.WebApi.Domain.Catalog.Brands;
using MediatR;

namespace DN.WebApi.Application.Catalog.Brands;

public class SearchBrandsRequest : PaginationFilter, IRequest<PaginationResponse<BrandDto>>
{
}

public class SearchBrandsRequestHandler : IRequestHandler<SearchBrandsRequest, PaginationResponse<BrandDto>>
{
    private readonly IReadRepository<Brand> _repository;

    public SearchBrandsRequestHandler(IReadRepository<Brand> repository) => _repository = repository;

    public async Task<PaginationResponse<BrandDto>> Handle(SearchBrandsRequest request, CancellationToken cancellationToken)
    {
        var spec = new EntitiesByPaginationFilterSpec<Brand, BrandDto>(request);

        var list = await _repository.ListAsync(spec, cancellationToken);
        int count = await _repository.CountAsync(spec, cancellationToken);

        return new PaginationResponse<BrandDto>(list, count, request.PageNumber, request.PageSize);
    }
}