using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class Search : EndpointBaseAsync
    .WithRequest<BrandListFilter>
    .WithResult<PaginatedResult<BrandDto>>
{
    private readonly IRepositoryAsync _repository;

    public Search(IRepositoryAsync repository) => _repository = repository;

    [HttpPost("search")]
    [MustHavePermission(PermissionConstants.Brands.Search)]
    [OpenApiOperation("Search brands using available filters.", "")]
    public override Task<PaginatedResult<BrandDto>> HandleAsync(BrandListFilter filter, CancellationToken cancellationToken = default)
    {
        var specification = new PaginationSpecification<Brand>
        {
            AdvancedSearch = filter.AdvancedSearch,
            Keyword = filter.Keyword,
            OrderBy = x => x.OrderBy(b => b.Name),
            OrderByStrings = filter.OrderBy,
            PageIndex = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return _repository.GetListAsync<Brand, BrandDto>(specification, cancellationToken);
    }
}