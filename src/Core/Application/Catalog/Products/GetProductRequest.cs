using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Domain.Catalog;
using MediatR;
using System.Linq.Expressions;

namespace DN.WebApi.Application.Catalog.Products;

public class GetProductRequest : IRequest<ProductDetailsDto>
{
    public Guid Id { get; set; }

    public GetProductRequest(Guid id) => Id = id;
}

public class GetProductRequestHandler : IRequestHandler<GetProductRequest, ProductDetailsDto>
{
    private readonly IRepositoryAsync _repository;

    public GetProductRequestHandler(IRepositoryAsync repository) => _repository = repository;

    public async Task<ProductDetailsDto> Handle(GetProductRequest request, CancellationToken cancellationToken)
    {
        var includes = new Expression<Func<Product, object>>[] { x => x.Brand };
        return await _repository.GetByIdAsync<Product, ProductDetailsDto>(request.Id, includes, cancellationToken);
    }
}