using FSH.Framework.Core.Identity.Tokens.Features.Generate;

namespace FSH.Framework.Core.Identity.Tokens;
public interface ITokenService
{
    Task<TokenGenerationResponse> GenerateTokenAsync(TokenGenerationCommand request, string ipAddress, CancellationToken cancellationToken);

}
