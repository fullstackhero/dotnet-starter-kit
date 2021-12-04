using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Catalog.Interfaces;

public interface IBrandService : ITransientService
{
    Task<PaginatedResult<BrandDto>> SearchAsync(BrandListFilter filter);

    Task<Result<Guid>> CreateBrandAsync(CreateBrandRequest request);

    Task<Result<Guid>> UpdateBrandAsync(UpdateBrandRequest request, Guid id);

    Task<Result<Guid>> DeleteBrandAsync(Guid id);

    Task<Result<string>> GenerateRandomBrandAsync(GenerateRandomBrandRequest request);

    Task<Result<string>> DeleteRandomBrandAsync();
}