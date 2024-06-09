using FSH.Blazor.Infrastructure.Auth.Jwt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Blazor.Infrastructure.Auth;
public static class Extensions
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<AuthenticationStateProvider, JwtAuthenticationService>()
                .AddScoped(sp => (IAuthenticationService)sp.GetRequiredService<AuthenticationStateProvider>())
                .AddScoped(sp => (IAccessTokenProvider)sp.GetRequiredService<AuthenticationStateProvider>())
                .AddScoped<IAccessTokenProviderAccessor, AccessTokenProviderAccessor>()
                .AddScoped<JwtAuthenticationHeaderHandler>();

        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        return services;
    }
}
