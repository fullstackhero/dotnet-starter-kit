using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Abstractions.Services.Catalog
{
    public interface IBrandService : ITransientService
    {
        Task<PaginatedResult<BrandDto>> GetBrandsAsync(BrandListFilter filter);
        Task<Result<Guid>> CreateBrandAsync(CreateBrandRequest request);
        Task<Result<Guid>> UpdateBrandAsync(UpdateBrandRequest request, Guid id);
        Task<Result<Guid>> DeleteBrandAsync(Guid id);
    }
}