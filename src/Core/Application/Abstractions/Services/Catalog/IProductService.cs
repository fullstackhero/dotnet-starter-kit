using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IProductService : ITransientService
    {
        Task<Result<ProductDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<ProductDetailsDto>> GetByIdUsingDapperAsync(Guid id);
        Task<Result<object>> CreateProductAsync(CreateProductRequest request);
    }
}