using Infrastructure.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Security.Claims;

namespace Infrastructure.Auth.Jwt;

// This is a client-side AuthenticationStateProvider that determines the user's authentication state by
// looking for data persisted in the page when it was rendered on the server. This authentication state will
// be fixed for the lifetime of the WebAssembly application. So, if the user needs to log in or out, a full
// page reload is required.
//
// This only provides a user name and email for display purposes. It does not actually include any tokens
// that authenticate to the server when making subsequent requests. That works separately using a
// cookie that will be included on HttpClient requests to the server.
public sealed class JwtAuthenticationService : AuthenticationStateProvider, IAuthenticationService, IAccessTokenProvider
{
    private static readonly Task<AuthenticationState> defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> authenticationStateTask = defaultUnauthenticatedTask;

    public JwtAuthenticationService(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
        {
            return;
        }

        authenticationStateTask = Task.FromResult(new AuthenticationState(userInfo.ToClaimsPrincipal()));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => authenticationStateTask;

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

    public ValueTask<AccessTokenResult> RequestAccessToken()
    {
        throw new NotImplementedException();
    }

    public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        throw new NotImplementedException();
    }
}
