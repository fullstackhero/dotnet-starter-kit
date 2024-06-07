using Infrastructure.Api;

namespace Infrastructure.Auth.Jwt;
public class JwtAuthenticationService : IAuthenticationService
{

    public Task<bool> LoginAsync(string tenantId, TokenGenerationCommand request)
    {
        throw new NotImplementedException();
    }

    public Task LogoutAsync()
    {
        throw new NotImplementedException();
    }

    public void NavigateToExternalLogin(string returnUrl)
    {
        throw new NotImplementedException();
    }

    public Task ReLoginAsync(string returnUrl)
    {
        throw new NotImplementedException();
    }
}
