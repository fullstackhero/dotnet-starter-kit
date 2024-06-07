using Infrastructure.Auth.Jwt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Auth;
public static class Extensions
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

        //services.AddScoped(sp => (IAccessTokenProvider)sp.GetRequiredService<AuthenticationStateProvider>())
        //        .AddScoped<IAccessTokenProviderAccessor, AccessTokenProviderAccessor>()
        //        .AddScoped<JwtAuthenticationHeaderHandler>();

        return services;
    }
}
