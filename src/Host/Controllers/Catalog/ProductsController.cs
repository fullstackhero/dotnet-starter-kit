using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Catalog;

[ApiConventionType(typeof(FSHApiConventions))]
public class ProductsController : BaseController
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.View)]
    public async Task<ActionResult<Result<ProductDetailsDto>>> GetAsync(Guid id)
    {
        var product = await _service.GetProductDetailsAsync(id);
        return Ok(product);
    }

    [HttpPost("search")]
    [MustHavePermission(PermissionConstants.Products.Search)]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> SearchAsync(ProductListFilter filter)
    {
        var products = await _service.SearchAsync(filter);
        return Ok(products);
    }

    [HttpGet("dapper")]
    [MustHavePermission(PermissionConstants.Products.View)]
    public async Task<ActionResult<Result<ProductDto>>> GetDapperAsync(Guid id)
    {
        var products = await _service.GetByIdUsingDapperAsync(id);
        return Ok(products);
    }

    [HttpPost]
    [MustHavePermission(PermissionConstants.Products.Register)]
    public async Task<ActionResult<Result<Guid>>> CreateAsync(CreateProductRequest request)
    {
        return Ok(await _service.CreateProductAsync(request));
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.Update)]
    public async Task<ActionResult<Result<Guid>>> UpdateAsync(UpdateProductRequest request, Guid id)
    {
        return Ok(await _service.UpdateProductAsync(request, id));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.Remove)]
    public async Task<ActionResult<Result<Guid>>> DeleteAsync(Guid id)
    {
        var productId = await _service.DeleteProductAsync(id);
        return Ok(productId);
    }
}