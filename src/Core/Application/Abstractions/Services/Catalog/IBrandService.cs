using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;
using System;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IBrandService : ITransientService
    {
        Task<PaginatedResult<BrandDto>> SearchAsync(BrandListFilter filter);
        Task<Result<Guid>> CreateBrandAsync(CreateBrandRequest request);
        Task<Result<Guid>> UpdateBrandAsync(UpdateBrandRequest request, Guid id);
        Task<Result<Guid>> DeleteBrandAsync(Guid id);
    }
}