using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Domain.Catalog;
using Mapster;
using MediatR;

namespace DN.WebApi.Application.Catalog.Products;

public class GetProductViaDapperRequest : IRequest<ProductDto>
{
    public Guid Id { get; set; }

    public GetProductViaDapperRequest(Guid id) => Id = id;
}

public class GetProductViaDapperRequestHandler : IRequestHandler<GetProductViaDapperRequest, ProductDto>
{
    private readonly IRepositoryAsync _repository;

    public GetProductViaDapperRequestHandler(IRepositoryAsync repository) => _repository = repository;

    public async Task<ProductDto> Handle(GetProductViaDapperRequest request, CancellationToken cancellationToken)
    {
        var product = await _repository.QueryFirstOrDefaultAsync<Product>(
            $"SELECT * FROM public.\"Products\" WHERE \"Id\"  = '{request.Id}' AND \"Tenant\" = '@tenant'", cancellationToken: cancellationToken);

        return product.Adapt<ProductDto>();
    }
}