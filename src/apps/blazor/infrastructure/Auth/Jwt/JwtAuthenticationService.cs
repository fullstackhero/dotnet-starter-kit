using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Infrastructure.Storage;
using FSH.Starter.Blazor.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace FSH.Starter.Blazor.Infrastructure.Auth.Jwt;

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
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IApiClient _client;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;

    public JwtAuthenticationService(PersistentComponentState state, ILocalStorageService localStorage, IApiClient client, NavigationManager navigation)
    {
        _localStorage = localStorage;
        _client = client;
        _navigation = navigation;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? cachedToken = await GetCachedAuthTokenAsync();
        if (string.IsNullOrWhiteSpace(cachedToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Generate claimsIdentity from cached token
        var claimsIdentity = new ClaimsIdentity(GetClaimsFromJwt(cachedToken), "jwt");

        // Add cached permissions as claims
        if (await GetCachedPermissionsAsync() is List<string> cachedPermissions)
        {
            claimsIdentity.AddClaims(cachedPermissions.Select(p => new Claim(FshClaims.Permission, p)));
        }

        return new AuthenticationState(new ClaimsPrincipal(claimsIdentity));
    }

    public async Task<bool> LoginAsync(string tenantId, TokenGenerationCommand request)
    {
        var tokenResponse = await _client.TokenGenerationEndpointAsync(tenantId, request);

        string? token = tokenResponse.Token;
        string? refreshToken = tokenResponse.RefreshToken;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        await CacheAuthTokens(token, refreshToken);

        // Get permissions for the current user and add them to the cache
        var permissions = await _client.GetUserPermissionsAsync();
        await CachePermissions(permissions);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

        return true;
    }

    public async Task LogoutAsync()
    {
        await ClearCacheAsync();

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

        _navigation.NavigateTo("/login");
    }

    public void NavigateToExternalLogin(string returnUrl)
    {
        throw new NotImplementedException();
    }

    public async Task ReLoginAsync(string returnUrl)
    {
        await LogoutAsync();
        _navigation.NavigateTo(returnUrl);
    }

    public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        return await RequestAccessToken();

    }

    public async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        // We make sure the access token is only refreshed by one thread at a time. The other ones have to wait.
        await _semaphore.WaitAsync();
        try
        {
            var authState = await GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated is not true)
            {
                return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, new(), "/login", default);
            }

            string? token = await GetCachedAuthTokenAsync();

            //// Check if token needs to be refreshed (when its expiration time is less than 1 minute away)
            var expTime = authState.User.GetExpiration();
            var diff = expTime - DateTime.UtcNow;
            if (diff.TotalMinutes <= 1)
            {
                //return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, new(), "/login", default);
                string? refreshToken = await GetCachedRefreshTokenAsync();
                (bool succeeded, var response) = await TryRefreshTokenAsync(new RefreshTokenCommand { Token = token, RefreshToken = refreshToken });
                if (!succeeded)
                {
                    return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, new(), "/login", default);
                }

                token = response?.Token;
            }

            return new AccessTokenResult(AccessTokenResultStatus.Success, new AccessToken() { Value = token! }, string.Empty, default);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<(bool Succeeded, TokenResponse? Token)> TryRefreshTokenAsync(RefreshTokenCommand request)
    {
        var authState = await GetAuthenticationStateAsync();
        string? tenantKey = authState.User.GetTenant();
        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            throw new InvalidOperationException("Can't refresh token when user is not logged in!");
        }

        try
        {
            var tokenResponse = await _client.RefreshTokenEndpointAsync(tenantKey, request);

            await CacheAuthTokens(tokenResponse.Token, tokenResponse.RefreshToken);

            return (true, tokenResponse);
        }
        catch
        {
            return (false, null);
        }
    }

    private async ValueTask CacheAuthTokens(string? token, string? refreshToken)
    {
        await _localStorage.SetItemAsync(StorageConstants.Local.AuthToken, token);
        await _localStorage.SetItemAsync(StorageConstants.Local.RefreshToken, refreshToken);
    }

    private ValueTask CachePermissions(ICollection<string> permissions)
    {
        return _localStorage.SetItemAsync(StorageConstants.Local.Permissions, permissions);
    }

    private async Task ClearCacheAsync()
    {
        await _localStorage.RemoveItemAsync(StorageConstants.Local.AuthToken);
        await _localStorage.RemoveItemAsync(StorageConstants.Local.RefreshToken);
        await _localStorage.RemoveItemAsync(StorageConstants.Local.Permissions);
    }
    private ValueTask<string?> GetCachedAuthTokenAsync()
    {
        return _localStorage.GetItemAsync<string?>(StorageConstants.Local.AuthToken);
    }

    private ValueTask<string?> GetCachedRefreshTokenAsync()
    {
        return _localStorage.GetItemAsync<string>(StorageConstants.Local.RefreshToken);
    }

    private ValueTask<ICollection<string>?> GetCachedPermissionsAsync()
    {
        return _localStorage.GetItemAsync<ICollection<string>>(StorageConstants.Local.Permissions);
    }

    private IEnumerable<Claim> GetClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        string payload = jwt.Split('.')[1];
        byte[] jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs is not null)
        {
            keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles);

            if (roles is not null)
            {
                string? rolesString = roles.ToString();
                if (!string.IsNullOrEmpty(rolesString))
                {
                    if (rolesString.Trim().StartsWith("["))
                    {
                        string[]? parsedRoles = JsonSerializer.Deserialize<string[]>(rolesString);

                        if (parsedRoles is not null)
                        {
                            claims.AddRange(parsedRoles.Select(role => new Claim(ClaimTypes.Role, role)));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, rolesString));
                    }
                }

                keyValuePairs.Remove(ClaimTypes.Role);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty)));
        }

        return claims;
    }
    private byte[] ParseBase64WithoutPadding(string payload)
    {
        payload = payload.Trim().Replace('-', '+').Replace('_', '/');
        string base64 = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }
}
