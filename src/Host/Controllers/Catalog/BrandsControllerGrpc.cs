using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Catalog;
using DN.WebApi.Shared.DTOs.Filters;
using GrpcShared.Controllers;
using GrpcShared.Models;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Catalog;

public class BrandsControllerGrpc : IBrandsControllerGrpc
{
    private readonly IBrandService _service;

    public BrandsControllerGrpc(IBrandService service)
    {
        _service = service;
    }

    [MustHavePermission(PermissionConstants.Brands.Search)]
    public async Task<PaginatedResult<BrandDto>> SearchAsync(BrandListFilter filter, CallContext context = default)
    {
        var brands = await _service.SearchAsync(filter);
        return brands;
    }

    [MustHavePermission(PermissionConstants.Brands.Register)]
    public async Task<Result<Guid>> CreateAsync(CreateBrandRequest request, CallContext context = default)
    {
        return await _service.CreateBrandAsync(request);
    }

    [MustHavePermission(PermissionConstants.Brands.Update)]
    public async Task<Result<Guid>> UpdateAsync(UpdateBrandRequestGrpc request, CallContext context = default)
    {
        return await _service.UpdateBrandAsync(request.Request, request.Id);
    }

    [MustHavePermission(PermissionConstants.Brands.Remove)]
    public async Task<Result<Guid>> DeleteAsync(GuidIdRequestGrpc request, CallContext context = default)
    {
        var brandId = await _service.DeleteBrandAsync(request.Id);
        return brandId;
    }

    public async Task<Result<string>> GenerateRandomBrandAsync(GenerateRandomBrandRequest request, CallContext context = default)
    {
        var jobId = await _service.GenerateRandomBrandAsync(request);
        return jobId;
    }

    public async Task<Result<string>> DeleteRandomAsync(CallContext context = default)
    {
        var jobId = await _service.DeleteRandomBrandAsync();
        return jobId;
    }
}
