using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Http;

namespace FSH.Framework.Identity.v1.Tokens.TokenGeneration;
internal class TokenGenerationCommandHandler(ITokenService tokenService, HttpContext context)
    : ICommandHandler<TokenGenerationCommand, TokenGenerationCommandResponse>
{
    public async Task<TokenGenerationCommandResponse> HandleAsync(TokenGenerationCommand command, CancellationToken cancellationToken = default)
    {
        string ip = context.GetIpAddress();
        var token = await tokenService.GenerateTokenAsync(command.Email, command.Password, ip, cancellationToken);
        return new TokenGenerationCommandResponse(token.Token, token.RefreshToken, token.RefreshTokenExpiryTime);
    }
}
