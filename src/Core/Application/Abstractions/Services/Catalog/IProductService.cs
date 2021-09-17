using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;
using DN.WebApi.Shared.DTOs.Filters;
using System;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IProductService : ITransientService
    {
        Task<Result<ProductDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<ProductDetailsDto>> GetByIdUsingDapperAsync(Guid id);
        Task<PaginatedResult<ProductDetailsDto>> GetListAsync(ProductListFilter filter);
        Task<Result<object>> CreateProductAsync(CreateProductRequest request);
        Task<Result<Guid>> DeleteProductAsync(Guid id);
    }
}