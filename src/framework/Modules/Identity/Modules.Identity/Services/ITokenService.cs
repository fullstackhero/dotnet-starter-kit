namespace FSH.Framework.Identity.Core.Tokens;
public interface ITokenService
{
    Task<TokenDto> GenerateTokenAsync(string email, string password, string ipAddress, CancellationToken cancellationToken);
    Task<TokenDto> RefreshTokenAsync(string token, string refreshToken, string ipAddress, CancellationToken cancellationToken);
}