using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Controllers;
using GrpcShared.Models;
using Microsoft.AspNetCore.Authorization;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Identity;

public class IdentityControllerGrpc : IIdentityControllerGrpc
{
    private readonly ICurrentUser _user;
    private readonly IIdentityService _identityService;
    private readonly IUserService _userService;

    public IdentityControllerGrpc(IIdentityService identityService, ICurrentUser user, IUserService userService)
    {
        _identityService = identityService;
        _user = user;
        _userService = userService;
    }

    [MustHavePermission(PermissionConstants.Identity.Register)]
    public async Task<IResult<string>> RegisterAsync(RegisterRequest request, CallContext context = default)
    {
        string origin = GenerateOrigin(context);
        return await _identityService.RegisterAsync(request, origin);
    }

    [AllowAnonymous]
    public async Task<IResult<string>> ConfirmEmailAsync(ConfirmEmailRequestGrpc request, CallContext context = default)
    {
        return await _identityService.ConfirmEmailAsync(request.UserId, request.Code, request.Tenant);
    }

    [AllowAnonymous]
    public async Task<IResult<string>> ConfirmPhoneNumberAsync(ConfirmPhoneNumberRequestGrpc request, CallContext context = default)
    {
        return await _identityService.ConfirmPhoneNumberAsync(request.UserId, request.Code);
    }

    [AllowAnonymous]
    public async Task<IResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request, CallContext context = default)
    {
        string origin = GenerateOrigin(context);
        return await _identityService.ForgotPasswordAsync(request, origin);
    }

    [AllowAnonymous]
    public async Task<IResult<string>> ResetPasswordAsync(ResetPasswordRequest request, CallContext context = default)
    {
        return await _identityService.ResetPasswordAsync(request);
    }

    public async Task<IResult<string>> UpdateProfileAsync(UpdateProfileRequest request, CallContext context = default)
    {
        return await _identityService.UpdateProfileAsync(request, _user.GetUserId().ToString());
    }

    public async Task<IResult<UserDetailsDto>> GetProfileDetailsAsync(CallContext context = default)
    {
        return await _userService.GetAsync(_user.GetUserId().ToString());
    }

    private string GenerateOrigin(CallContext context)
    {
        string baseUrl = $"https://{context.ServerCallContext.Host}";
        var horigin = context.RequestHeaders.FirstOrDefault(i => i.Key == "origin");
        string origin = horigin == null || string.IsNullOrEmpty(horigin.Value) ? baseUrl : horigin.Value;
        return origin;
    }
}
