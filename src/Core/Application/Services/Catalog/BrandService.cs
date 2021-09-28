using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Application.Abstractions.Services.Catalog;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Entities.Catalog;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Services.Catalog
{
    public class BrandService : IBrandService
    {
        private readonly IStringLocalizer<BrandService> _localizer;
        private readonly IRepositoryAsync _repository;

        public BrandService(IRepositoryAsync repository, IStringLocalizer<BrandService> localizer)
        {
            _repository = repository;
            _localizer = localizer;
        }

        public async Task<Result<object>> CreateBrandAsync(CreateBrandRequest request)
        {
            var brandExists = await _repository.ExistsAsync<Brand>(a => a.Name == request.Name);
            if (brandExists) throw new EntityAlreadyExistsException(string.Format(_localizer["brand.alreadyexists"], request.Name));
            var brand = new Brand(request.Name, request.Description);
            var brandId = await _repository.CreateAsync<Brand>(brand);
            await _repository.SaveChangesAsync();
            return await Result<object>.SuccessAsync(brandId);
        }

        public Task<Result<Guid>> DeleteBrandAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<PaginatedResult<BrandDto>> GetBrandsAsync(BrandListFilter filter)
        {
            var brands = await _repository.GetPaginatedListAsync<Brand, BrandDto>(filter.PageNumber, filter.PageSize, filter.OrderBy, filter.Search);
            return brands;
        }

        public Task<Result<object>> UpdateBrandAsync(UpdateBrandRequest request, Guid id)
        {
            throw new NotImplementedException();
        }
    }
}