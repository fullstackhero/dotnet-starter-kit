using FSH.Framework.Identity.Contracts.v1.Tokens.RefreshToken;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using FSH.Modules.Common.Core.Messaging.CQRS;
using Microsoft.AspNetCore.Http;

namespace FSH.Framework.Identity.v1.Tokens.RefreshToken;
internal sealed class RefreshTokenCommandHandler(
    ITokenService tokenService,
    HttpContext context)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenCommandResponse>
{
    public async Task<RefreshTokenCommandResponse> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        string ip = context.GetIpAddress();
        var token = await tokenService.RefreshTokenAsync(command.Token, command.RefreshToken, ip, cancellationToken);
        return new RefreshTokenCommandResponse(token.Token, token.RefreshToken, token.RefreshTokenExpiryTime);
    }
}