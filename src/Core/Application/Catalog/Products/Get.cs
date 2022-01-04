using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System.Linq.Expressions;

namespace DN.WebApi.Application.Catalog.Products;

public class Get : EndpointBaseAsync
    .WithRequest<GetProductRequest>
    .WithResult<Result<ProductDetailsDto>>
{
    private readonly IRepositoryAsync _repository;

    public Get(IRepositoryAsync repository) => _repository = repository;

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionConstants.Products.View)]
    [OpenApiOperation("Get product details.", "")]
    public override async Task<Result<ProductDetailsDto>> HandleAsync([FromRoute] GetProductRequest request, CancellationToken cancellationToken = default)
    {
        var includes = new Expression<Func<Product, object>>[] { x => x.Brand };
        var product = await _repository.GetByIdAsync<Product, ProductDetailsDto>(request.Id, includes);
        return await Result<ProductDetailsDto>.SuccessAsync(product);
    }
}