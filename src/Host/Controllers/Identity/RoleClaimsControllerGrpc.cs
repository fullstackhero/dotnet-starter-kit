using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Controllers;
using GrpcShared.Models;
using Microsoft.AspNetCore.Authorization;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Identity;

public class RoleClaimsControllerGrpc : IRoleClaimsControllerGrpc
{
    private readonly IRoleClaimsService _roleClaimService;

    public RoleClaimsControllerGrpc(IRoleClaimsService roleClaimService)
    {
        _roleClaimService = roleClaimService;
    }

    [Authorize(Policy = PermissionConstants.RoleClaims.View)]
    public async Task<Result<List<RoleClaimResponse>>> GetAllAsync(CallContext context = default)
    {
        var roleClaims = await _roleClaimService.GetAllAsync();
        return roleClaims;
    }

    [Authorize(Policy = PermissionConstants.RoleClaims.View)]
    public async Task<Result<List<RoleClaimResponse>>> GetAllByRoleIdAsync(string roleId, CallContext context = default)
    {
        var response = await _roleClaimService.GetAllByRoleIdAsync(roleId);
        return response;
    }

    [Authorize(Policy = PermissionConstants.RoleClaims.Create)]
    public async Task<Result<string>> PostAsync(RoleClaimRequest request, CallContext context = default)
    {
        var response = await _roleClaimService.SaveAsync(request);
        return response;
    }

    [Authorize(Policy = PermissionConstants.RoleClaims.Delete)]
    public async Task<Result<string>> DeleteAsync(DeleteRequestGrpc request, CallContext context = default)
    {
        var response = await _roleClaimService.DeleteAsync(request.Id);
        return response;
    }
}
