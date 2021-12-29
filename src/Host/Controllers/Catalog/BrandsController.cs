using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Controllers.Catalog;

[ApiConventionType(typeof(FSHApiConventions))]
public class BrandsController : BaseController
{
    private readonly IBrandService _service;

    public BrandsController(IBrandService service)
    {
        _service = service;
    }

    [HttpPost("search")]
    [MustHavePermission(PermissionConstants.Brands.Search)]
    [OpenApiOperation("Search Brands using available Filters.", "")]
    public async Task<ActionResult<PaginatedResult<BrandDto>>> SearchAsync(BrandListFilter filter)
    {
        var brands = await _service.SearchAsync(filter);
        return Ok(brands);
    }

    [HttpPost]
    [MustHavePermission(PermissionConstants.Brands.Register)]
    public async Task<ActionResult<Result<Guid>>> CreateAsync(CreateBrandRequest request)
    {
        return Ok(await _service.CreateBrandAsync(request));
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(PermissionConstants.Brands.Update)]
    public async Task<ActionResult<Result<Guid>>> UpdateAsync(UpdateBrandRequest request, Guid id)
    {
        return Ok(await _service.UpdateBrandAsync(request, id));
    }

    [HttpDelete("{id:guid}")]
    [MustHavePermission(PermissionConstants.Brands.Remove)]
    public async Task<ActionResult<Result<Guid>>> DeleteAsync(Guid id)
    {
        var brandId = await _service.DeleteBrandAsync(id);
        return Ok(brandId);
    }

    [HttpPost("generate-random")]
    public async Task<ActionResult<Result<string>>> GenerateRandomAsync(GenerateRandomBrandRequest request)
    {
        var jobId = await _service.GenerateRandomBrandAsync(request);
        return Ok(jobId);
    }

    [HttpDelete("delete-random")]
    [ProducesResponseType(200)]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    public async Task<ActionResult<Result<string>>> DeleteRandomAsync()
    {
        var jobId = await _service.DeleteRandomBrandAsync();
        return Ok(jobId);
    }
}