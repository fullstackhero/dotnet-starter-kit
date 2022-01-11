using DN.WebApi.Application.Common.Models;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Application.Common.Specification;
using DN.WebApi.Domain.Catalog.Brands;
using Mapster;
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
        var spec = new ItemsByPaginationFilterSpec<Brand>(request);

        var list = await _repository.ListAsync(spec, cancellationToken);
        int count = await _repository.CountAsync(spec, cancellationToken);

        return PaginationResponse<BrandDto>.Create(list.Adapt<List<BrandDto>>(), count, request.PageNumber, request.PageSize);
    }
}