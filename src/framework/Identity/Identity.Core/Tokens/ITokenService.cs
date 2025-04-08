using FSH.Framework.Identity.Core.Dtos;

namespace FSH.Framework.Identity.Core.Tokens;
public interface ITokenService
{
    Task<TokenDto> GenerateTokenAsync(TokenGenerationRequest request, string ipAddress, CancellationToken cancellationToken);
    Task<TokenDto> RefreshTokenAsync(TokenRefreshRequest request, string ipAddress, CancellationToken cancellationToken);

}
