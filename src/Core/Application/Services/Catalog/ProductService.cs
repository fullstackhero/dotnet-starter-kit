using AutoMapper;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Application.Abstractions.Services.Catalog;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Entities.Catalog;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.Extensions.Localization;
using System;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Services.Catalog
{
    public class ProductService : IProductService
    {
        private readonly IStringLocalizer<ProductService> _localizer;
        private readonly IMapper _mapper;
        private readonly IRepositoryAsync _repository;

        public ProductService(IRepositoryAsync repository, IMapper mapper, IStringLocalizer<ProductService> localizer)
        {
            _repository = repository;
            _mapper = mapper;
            _localizer = localizer;
        }

        public async Task<Result<object>> CreateProductAsync(CreateProductRequest request)
        {
            var productExists = await _repository.ExistsAsync<Product>(a => a.Name == request.Name);
            if (productExists) throw new EntityAlreadyExistsException(string.Format(_localizer["product.alreadyexists"], request.Name));
            var product = new Product(request.Name, request.Description, request.Rate);
            var productId = await _repository.CreateAsync<Product>(product);
            await _repository.SaveChangesAsync();
            return await Result<object>.SuccessAsync(productId);
        }

        public async Task<Result<Guid>> DeleteProductAsync(Guid id)
        {
            await _repository.RemoveByIdAsync<Product>(id);
            await _repository.SaveChangesAsync();
            return await Result<Guid>.SuccessAsync(id);
        }

        public async Task<Result<ProductDetailsDto>> GetByIdAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync<Product, ProductDetailsDto>(id);
            return await Result<ProductDetailsDto>.SuccessAsync(product);
        }

        public async Task<Result<ProductDetailsDto>> GetByIdUsingDapperAsync(Guid id)
        {
            var product = await _repository.QueryFirstOrDefaultAsync<Product>($"SELECT * FROM public.\"Products\" WHERE \"Id\"  = '{id}' AND \"TenantKey\"='@tenantKey'");
            var mappedProduct = _mapper.Map<ProductDetailsDto>(product);
            return await Result<ProductDetailsDto>.SuccessAsync(mappedProduct);
        }

        public async Task<PaginatedResult<ProductDetailsDto>> GetListAsync(int pageNumber, int pageSize, string[] orderBy)
        {
            var products = await _repository.GetPaginatedListAsync<Product, ProductDetailsDto>(pageNumber, pageSize, orderBy);
            return products;
        }
    }
}