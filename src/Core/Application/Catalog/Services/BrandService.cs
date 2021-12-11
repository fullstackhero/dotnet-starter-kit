using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Services;

public class BrandService : IBrandService
{
    private readonly IStringLocalizer<BrandService> _localizer;
    private readonly IRepositoryAsync _repository;
    private readonly IJobService _jobService;

    public BrandService(IRepositoryAsync repository, IStringLocalizer<BrandService> localizer, IJobService jobService)
    {
        _repository = repository;
        _localizer = localizer;
        _jobService = jobService;
    }

    public async Task<Result<Guid>> CreateBrandAsync(CreateBrandRequest request)
    {
        bool brandExists = await _repository.ExistsAsync<Brand>(a => a.Name == request.Name);
        if (brandExists) throw new EntityAlreadyExistsException(string.Format(_localizer["brand.alreadyexists"], request.Name));
        var brand = new Brand(request.Name, request.Description);
        brand.DomainEvents.Add(new StatsChangedEvent());
        var brandId = await _repository.CreateAsync(brand);
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(brandId);
    }

    public async Task<Result<Guid>> DeleteBrandAsync(Guid id)
    {
        var brandToDelete = await _repository.RemoveByIdAsync<Brand>(id);
        brandToDelete.DomainEvents.Add(new StatsChangedEvent());
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(id);
    }

    public Task<PaginatedResult<BrandDto>> SearchAsync(BrandListFilter filter)
    {
        var specification = new PaginationSpecification<Brand>
        {
            AdvancedSearch = filter.AdvancedSearch,
            Keyword = filter.Keyword,
            OrderBy = x => x.OrderBy(b => b.Name),
            OrderByStrings = filter.OrderBy,
            PageIndex = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return _repository.GetListAsync<Brand, BrandDto>(specification);
    }

    public async Task<Result<Guid>> UpdateBrandAsync(UpdateBrandRequest request, Guid id)
    {
        var brand = await _repository.GetByIdAsync<Brand>(id);
        if (brand == null) throw new EntityNotFoundException(string.Format(_localizer["brand.notfound"], id));
        var updatedBrand = brand.Update(request.Name, request.Description);
        updatedBrand.DomainEvents.Add(new StatsChangedEvent());
        await _repository.UpdateAsync(updatedBrand);
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(id);
    }

    public Task<Result<string>> GenerateRandomBrandAsync(GenerateRandomBrandRequest request)
    {
        string jobId = _jobService.Enqueue<IBrandGeneratorJob>(x => x.GenerateAsync(request.NSeed));
        return Result<string>.SuccessAsync(jobId);
    }

    public Task<Result<string>> DeleteRandomBrandAsync()
    {
        string jobId = _jobService.Schedule<IBrandGeneratorJob>(x => x.CleanAsync(), TimeSpan.FromSeconds(5));
        return Result<string>.SuccessAsync(jobId);
    }
}