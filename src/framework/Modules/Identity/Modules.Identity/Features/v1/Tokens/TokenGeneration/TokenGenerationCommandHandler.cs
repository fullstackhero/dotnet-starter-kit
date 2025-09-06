using FSH.Framework.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using FSH.Modules.Common.Core.Messaging.CQRS;
using Microsoft.AspNetCore.Http;

namespace FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
public class TokenGenerationCommandHandler(
    ITokenService tokenService,
    IHttpContextAccessor contextAccessor)
    : ICommandHandler<TokenGenerationCommand, TokenGenerationCommandResponse>
{
    public async Task<TokenGenerationCommandResponse> HandleAsync(TokenGenerationCommand command, CancellationToken cancellationToken = default)
    {
        string? ip = contextAccessor.HttpContext?.GetIpAddress() ?? "unknown";
        var token = await tokenService.GenerateTokenAsync(command.Email, command.Password, ip, cancellationToken);
        return new TokenGenerationCommandResponse(token.Token, token.RefreshToken, token.RefreshTokenExpiryTime);
    }
}