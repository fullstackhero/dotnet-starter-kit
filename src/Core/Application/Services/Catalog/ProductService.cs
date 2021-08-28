using AutoMapper;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Application.Abstractions.Services.Catalog;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Entities.Catalog;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Services.Catalog
{
    public class ProductService : IProductService
    {
        private readonly IMapper _mapper;
        private readonly IRepository _repository;

        public ProductService(IRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<Result<object>> CreateProductAsync(CreateProductRequest request)
        {
            var productExists = await _repository.ExistsAsync<Product>(a => a.Name == request.Name);
            if (productExists) throw new EntityAlreadyExistsException<Product>(nameof(request.Name), request.Name);
            var product = new Product(request.Name, request.Description, request.Rate);
            var productId = await _repository.CreateAsync<Product>(product);
            await _repository.SaveChangesAsync();
            return await Result<object>.SuccessAsync(productId);
        }

        public async Task<Result<ProductDetailsDto>> GetById(Guid id)
        {
            var product = await _repository.GetCachedDtoByIdAsync<Product, ProductDetailsDto>(id);
            return await Result<ProductDetailsDto>.SuccessAsync(product);
        }

        public async Task<Result<ProductDetailsDto>> GetByIdDapper(Guid id)
        {
            //Dapper isn't advanced enough to support MultiTenancy
            //Workaround - In Repository Layer, I check if T implements IMustHaveTenant Interface. If so, replaces @tenantId with currentTenantId in the SQL query.
            //Not a clean way, but works.
            //Make sure to include TenantId='@tenantId' in your queries.
            var product = await _repository.QueryFirstOrDefaultAsync<Product>($"SELECT * FROM public.\"Products\" WHERE \"Id\"  = '{id}' AND \"TenantId\"='@tenantId'");
            var mappedProduct = _mapper.Map<ProductDetailsDto>(product);
            return await Result<ProductDetailsDto>.SuccessAsync(mappedProduct);
        }
    }
}