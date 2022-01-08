using DN.WebApi.Application.Catalog.Products;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Controllers.Catalog;

public class ProductsController : VersionedApiController
{
    [HttpPost("search")]
    [MustHavePermission(PermissionConstants.Products.Search)]
    [OpenApiOperation("Search products using available filters.", "")]
    public Task<PaginatedResult<ProductDto>> SearchAsync(SearchProductsRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.View)]
    [OpenApiOperation("Get product details.", "")]
    public Task<ProductDetailsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetProductRequest(id));
    }

    [HttpGet("dapper")]
    [MustHavePermission(PermissionConstants.Products.View)]
    [OpenApiOperation("Get product details via dapper.", "")]
    public Task<ProductDto> GetDapperAsync(Guid id)
    {
        return Mediator.Send(new GetProductViaDapperRequest(id));
    }

    [HttpPost]
    [MustHavePermission(PermissionConstants.Products.Register)]
    [OpenApiOperation("Create a new product.", "")]
    public Task<Guid> CreateAsync(CreateProductRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.Update)]
    [OpenApiOperation("Update a product.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateProductRequest request, Guid id)
    {
        if (id != request.Id)
        {
            return BadRequest();
        }

        return Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.Remove)]
    [OpenApiOperation("Delete a product.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteProductRequest(id));
    }
}