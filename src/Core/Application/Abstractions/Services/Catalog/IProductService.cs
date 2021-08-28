using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IProductService : IApplicationService
    {
        Task<Result<ProductDetailsDto>> GetById(Guid id);
        Task<Result<object>> CreateProductAsync(CreateProductRequest request);
    }
}