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

        public async Task<Result<ProductDetailsDto>> GetById(Guid id)
        {
            var product = await _repository.GetCachedDtoByIdAsync<Product,ProductDetailsDto>(id);
            return await Result<ProductDetailsDto>.SuccessAsync(product);
        }
    }
}