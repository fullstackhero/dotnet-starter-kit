using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Http;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Generate;
public sealed class TokenGenerationHandler(
    ITokenService tokenService,
    HttpContext context)
    : ICommandHandler<TokenGenerationCommand, TokenGenerationResponse>
{
    public async Task<TokenGenerationResponse> HandleAsync(TokenGenerationCommand command, CancellationToken cancellationToken = default)
    {
        var request = new TokenGenerationRequest(command.Email, command.Password);
        string ip = context.GetIpAddress();

        var token = await tokenService.GenerateTokenAsync(request, ip, cancellationToken);

        return new TokenGenerationResponse(token.Token, token.RefreshToken, token.RefreshTokenExpiryTime);
    }
}
