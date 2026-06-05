using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Authorization.Jwt;

internal static class JwtAuthenticationExtensions
{
    internal static IServiceCollection ConfigureJwtAuth(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(nameof(JwtOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services
            .AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, null!);

        services.AddAuthorizationBuilder().AddRequiredPermissionPolicy();
        services.AddAuthorization(options =>
        {
            // Permission evaluation lives in the RequiredPermission policy (it reads each
            // endpoint's RequiredPermissionAttribute metadata). Wire it as BOTH the default
            // AND the fallback policy:
            //   - FallbackPolicy covers endpoints with no auth metadata at all.
            //   - DefaultPolicy covers endpoints that opt in via .RequireAuthorization() —
            //     including the module route-groups (Catalog/Billing/Chat/Files/…). Without
            //     this, a group-level .RequireAuthorization() applied the built-in
            //     authenticated-only default, which SUPPRESSED the fallback, so
            //     .RequirePermission(...) was never evaluated and any authenticated tenant
            //     member could perform gated writes. Both must point at the permission policy.
            options.DefaultPolicy = options.GetPolicy(RequiredPermissionDefaults.PolicyName)!;
            options.FallbackPolicy = options.GetPolicy(RequiredPermissionDefaults.PolicyName);
        });
        return services;
    }
}