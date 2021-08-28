using AutoMapper;
using DN.WebApi.Application.Abstractions.Services.Catalog;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Interfaces;
using DN.WebApi.Shared.DTOs.Catalog;
using MediatR;

namespace DN.WebApi.Application.Services.Catalog
{
    public class ProductService : IProductService
    {
        private readonly IMapper _mapper;
        private readonly IProductRepository _repository;
        private readonly IMediator _mediator;

        public ProductService(IProductRepository repository, IMapper mapper, IMediator mediator)
        {
            _repository = repository;
            _mapper = mapper;
            _mediator = mediator;
        }

        public async Task<Result<ProductDetailsDto>> GetById(Guid id)
        {
            var product = _mapper.Map<ProductDetailsDto>(await _repository.GetById(id));
            return await Result<ProductDetailsDto>.SuccessAsync(product);
        }
    }
}