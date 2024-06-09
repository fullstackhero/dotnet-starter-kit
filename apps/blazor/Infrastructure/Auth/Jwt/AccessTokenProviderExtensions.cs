using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace FSH.Blazor.Infrastructure.Auth.Jwt;

public static class AccessTokenProviderExtensions
{
    public static async Task<string?> GetAccessTokenAsync(this IAccessTokenProvider tokenProvider) =>
        (await tokenProvider.RequestAccessToken())
            .TryGetToken(out var token)
                ? token.Value
                : null;
}