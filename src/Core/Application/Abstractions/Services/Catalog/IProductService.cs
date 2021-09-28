using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;
using DN.WebApi.Shared.DTOs.Filters;
using System;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IProductService : ITransientService
    {
        Task<Result<ProductDetailsDto>> GetProductDetailsAsync(Guid id);
        Task<Result<ProductDto>> GetByIdUsingDapperAsync(Guid id);
        Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductListFilter filter);
        Task<Result<object>> CreateProductAsync(CreateProductRequest request);
        Task<Result<object>> UpdateProductAsync(UpdateProductRequest request, Guid id);
        Task<Result<Guid>> DeleteProductAsync(Guid id);
    }
}