using DN.WebApi.Application.Abstractions.Jobs;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Application.Abstractions.Services.Catalog;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Entities.Catalog;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.Extensions.Localization;
using System;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Services.Catalog
{
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
            var brandId = await _repository.CreateAsync<Brand>(brand);
            await _repository.SaveChangesAsync();
            return await Result<Guid>.SuccessAsync(brandId);
        }

        public async Task<Result<Guid>> DeleteBrandAsync(Guid id)
        {
            await _repository.RemoveByIdAsync<Brand>(id);
            await _repository.SaveChangesAsync();
            return await Result<Guid>.SuccessAsync(id);
        }

        public async Task<PaginatedResult<BrandDto>> SearchAsync(BrandListFilter filter)
        {
            var brands = await _repository.GetSearchResultsAsync<Brand, BrandDto>(filter.PageNumber, filter.PageSize, filter.OrderBy, filter.AdvancedSearch, filter.Keyword);
            return brands;
        }

        public async Task<Result<Guid>> UpdateBrandAsync(UpdateBrandRequest request, Guid id)
        {
            var brand = await _repository.GetByIdAsync<Brand>(id);
            if (brand == null) throw new EntityNotFoundException(string.Format(_localizer["brand.notfound"], id));
            var updatedBrand = brand.Update(request.Name, request.Description);
            await _repository.UpdateAsync<Brand>(updatedBrand);
            await _repository.SaveChangesAsync();
            return await Result<Guid>.SuccessAsync(id);
        }

        public async Task<Result<string>> GenerateRandomBrandAsync(GenerateRandomBrandRequest request)
        {
            string jobId = _jobService.Enqueue<IBrandGeneratorJob>(x => x.GenerateAsync(request.NSeed));
            return await Result<string>.SuccessAsync(jobId);
        }

        public async Task<Result<string>> DeleteRandomBrandAsync()
        {
            string jobId = _jobService.Schedule<IBrandGeneratorJob>(x => x.CleanAsync(), TimeSpan.FromSeconds(5));
            return await Result<string>.SuccessAsync(jobId);
        }
    }
}