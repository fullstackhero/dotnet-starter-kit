using DN.WebApi.Application.Common;
using DN.WebApi.Application.Wrapper;

namespace DN.WebApi.Application.Identity.Tokens;

public interface ITokenService : ITransientService
{
    Task<IResult<TokenResponse>> GetTokenAsync(TokenRequest request, string ipAddress);

    Task<IResult<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress);
}