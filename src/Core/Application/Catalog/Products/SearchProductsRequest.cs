using DN.WebApi.Application.Common.Models;
using DN.WebApi.Application.Common.Persistance;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Domain.Catalog.Products;
using DN.WebApi.Domain.Common.Contracts;
using MediatR;
using System.Linq.Expressions;

namespace DN.WebApi.Application.Catalog.Products;

public class SearchProductsRequest : PaginationFilter, IRequest<PaginationResponse<ProductDto>>
{
    public Guid? BrandId { get; set; }
    public decimal? MinimumRate { get; set; }
    public decimal? MaximumRate { get; set; }
}

public class SearchProductsRequestHandler : IRequestHandler<SearchProductsRequest, PaginationResponse<ProductDto>>
{
    private readonly IRepositoryAsync _repository;

    public SearchProductsRequestHandler(IRepositoryAsync repository) => _repository = repository;

    public Task<PaginationResponse<ProductDto>> Handle(SearchProductsRequest request, CancellationToken cancellationToken)
    {
        var filters = new Filters<Product>();
        filters.Add(request.BrandId.HasValue, x => x.BrandId.Equals(request.BrandId!.Value));
        filters.Add(request.MinimumRate.HasValue, x => x.Rate >= request.MinimumRate!.Value);
        filters.Add(request.MaximumRate.HasValue, x => x.Rate <= request.MaximumRate!.Value);

        var specification = new PaginationSpecification<Product>
        {
            AdvancedSearch = request.AdvancedSearch,
            Filters = filters,
            Keyword = request.Keyword,
            OrderBy = x => x.OrderBy(b => b.Name),
            OrderByStrings = request.OrderBy,
            PageIndex = request.PageNumber,
            PageSize = request.PageSize,
            Includes = new Expression<Func<Product, object>>[] { x => x.Brand }
        };

        return _repository.GetListAsync<Product, ProductDto>(specification, cancellationToken);
    }
}