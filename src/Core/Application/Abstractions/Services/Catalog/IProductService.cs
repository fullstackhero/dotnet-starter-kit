using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IProductService
    {
        Task<Result<ProductDetailsDto>> GetById(Guid id);
    }
}