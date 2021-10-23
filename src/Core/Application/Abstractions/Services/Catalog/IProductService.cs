using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;
using System;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IProductService : ITransientService
    {
        Task<Result<ProductDetailsDto>> GetProductDetailsAsync(Guid id);
        Task<Result<ProductDto>> GetByIdUsingDapperAsync(Guid id);
        Task<PaginatedResult<ProductDto>> SearchAsync(ProductListFilter filter);
        Task<Result<Guid>> CreateProductAsync(CreateProductRequest request);
        Task<Result<Guid>> UpdateProductAsync(UpdateProductRequest request, Guid id);
        Task<Result<Guid>> DeleteProductAsync(Guid id);
    }
}