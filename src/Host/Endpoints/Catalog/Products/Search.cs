using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System.Linq.Expressions;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

public class Search : EndpointBaseAsync
    .WithRequest<ProductListFilter>
    .WithResult<PaginatedResult<ProductDto>>
{
    private readonly IRepositoryAsync _repository;

    public Search(IRepositoryAsync repository) => _repository = repository;

    [HttpPost("search")]
    [MustHavePermission(PermissionConstants.Products.Search)]
    [OpenApiOperation("Search products using available filters.", "")]
    public override Task<PaginatedResult<ProductDto>> HandleAsync(ProductListFilter filter, CancellationToken cancellationToken = default)
    {
        var filters = new Filters<Product>();
        filters.Add(filter.BrandId.HasValue, x => x.BrandId.Equals(filter.BrandId!.Value));
        filters.Add(filter.MinimumRate.HasValue, x => x.Rate >= filter.MinimumRate!.Value);
        filters.Add(filter.MaximumRate.HasValue, x => x.Rate <= filter.MaximumRate!.Value);

        var specification = new PaginationSpecification<Product>
        {
            AdvancedSearch = filter.AdvancedSearch,
            Filters = filters,
            Keyword = filter.Keyword,
            OrderBy = x => x.OrderBy(b => b.Name),
            OrderByStrings = filter.OrderBy,
            PageIndex = filter.PageNumber,
            PageSize = filter.PageSize,
            Includes = new Expression<Func<Product, object>>[] { x => x.Brand }
        };

        return _repository.GetListAsync<Product, ProductDto>(specification, cancellationToken);
    }
}