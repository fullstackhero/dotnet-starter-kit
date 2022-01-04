using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

public class GetDapper : EndpointBaseAsync
    .WithRequest<IdFromRoute>
    .WithResult<Result<ProductDto>>
{
    private readonly IRepositoryAsync _repository;

    public GetDapper(IRepositoryAsync repository) => _repository = repository;

    [HttpGet("dapper/{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.View)]
    [OpenApiOperation("Get product details via dapper.", "")]
    public override async Task<Result<ProductDto>> HandleAsync([FromRoute] IdFromRoute request, CancellationToken cancellationToken = default)
    {
        var product = await _repository.QueryFirstOrDefaultAsync<Product>(
            $"SELECT * FROM public.\"Products\" WHERE \"Id\"  = '{request.Id}' AND \"Tenant\" = '@tenant'", cancellationToken: cancellationToken);
        var mappedProduct = product.Adapt<ProductDto>();
        return Result<ProductDto>.Success(mappedProduct);
    }
}