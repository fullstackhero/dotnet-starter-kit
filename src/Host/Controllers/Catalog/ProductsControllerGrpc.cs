using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Catalog;
using GrpcShared.Controllers;
using GrpcShared.Models;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Catalog;

public class ProductsControllerGrpc : IProductsControllerGrpc
{
    private readonly IProductService _service;

    public ProductsControllerGrpc(IProductService service)
    {
        _service = service;
    }

    [MustHavePermission(PermissionConstants.Products.View)]
    public async Task<Result<ProductDetailsDto>> GetAsync(GuidIdRequestGrpc request, CallContext context = default)
    {
        var product = await _service.GetProductDetailsAsync(request.Id);
        return product;
    }

    [MustHavePermission(PermissionConstants.Products.Search)]
    public async Task<PaginatedResult<ProductDto>> SearchAsync(ProductListFilter filter, CallContext context = default)
    {
        var products = await _service.SearchAsync(filter);
        return products;
    }

    [MustHavePermission(PermissionConstants.Products.View)]
    public async Task<Result<ProductDto>> GetDapperAsync(GuidIdRequestGrpc request, CallContext context = default)
    {
        var products = await _service.GetByIdUsingDapperAsync(request.Id);
        return products;
    }

    [MustHavePermission(PermissionConstants.Products.Register)]
    public async Task<Result<Guid>> CreateAsync(CreateProductRequest request, CallContext context = default)
    {
        return await _service.CreateProductAsync(request);
    }

    [MustHavePermission(PermissionConstants.Products.Update)]
    public async Task<Result<Guid>> UpdateAsync(UpdateProductRequestGrpc request, CallContext context = default)
    {
        return await _service.UpdateProductAsync(request.Request, request.Id);
    }

    [MustHavePermission(PermissionConstants.Products.Remove)]
    public async Task<Result<Guid>> DeleteAsync(GuidIdRequestGrpc request, CallContext context = default)
    {
        var productId = await _service.DeleteProductAsync(request.Id);
        return productId;
    }
}
