using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using MediatR;
using System.Linq.Expressions;

namespace DN.WebApi.Application.Catalog.Products;

public class GetProductRequest : IRequest<Result<ProductDetailsDto>>
{
    public Guid Id { get; set; }

    public GetProductRequest(Guid id) => Id = id;
}

public class GetProductRequestHandler : IRequestHandler<GetProductRequest, Result<ProductDetailsDto>>
{
    private readonly IRepositoryAsync _repository;

    public GetProductRequestHandler(IRepositoryAsync repository) => _repository = repository;

    public async Task<Result<ProductDetailsDto>> Handle(GetProductRequest request, CancellationToken cancellationToken)
    {
        var includes = new Expression<Func<Product, object>>[] { x => x.Brand };
        var product = await _repository.GetByIdAsync<Product, ProductDetailsDto>(request.Id, includes, cancellationToken);

        return Result<ProductDetailsDto>.Success(product);
    }
}