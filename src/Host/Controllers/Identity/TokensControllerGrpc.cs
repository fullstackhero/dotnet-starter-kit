using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Controllers;
using Microsoft.AspNetCore.Authorization;
using ProtoBuf.Grpc;
using System.Net;

namespace DN.WebApi.Host.Controllers.Identity;

public class TokensControllerGrpc : ITokensControllerGrpc
{
    private readonly ITokenService _tokenService;

    public TokensControllerGrpc(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    public async Task<TokenResponse> GetTokenAsync(TokenRequest request, CallContext context)
    {
        var token = await _tokenService.GetTokenAsync(request, GenerateIPAddress(context));
        return token.Data;
    }

    [AllowAnonymous]
    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CallContext context)
    {
        var response = await _tokenService.RefreshTokenAsync(request, GenerateIPAddress(context));
        return response.Data;
    }

    private string GenerateIPAddress(CallContext context)
    {
        string host = new Uri(context.ServerCallContext.Peer).Host;
        if (IPAddress.TryParse(host, out IPAddress iP))
            return iP.MapToIPv4().ToString();

        return host;
    }
}
