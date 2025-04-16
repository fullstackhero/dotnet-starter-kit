using FSH.Framework.Core.ExecutionContext;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Identity.Contracts;
using FSH.Framework.Identity.Core.Roles;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Identity.Core.Users;
using FSH.Framework.Identity.Infrastructure.Data;
using FSH.Framework.Identity.Infrastructure.Roles;
using FSH.Framework.Identity.Infrastructure.Tokens;
using FSH.Framework.Identity.Infrastructure.Users;
using FSH.Framework.Identity.v1.Tokens.TokenGeneration;
using FSH.Framework.Infrastructure.Auth;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Identity.Users.Services;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Identity;
public static class IdentityModule
{
    public static IServiceCollection RegisterIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<CurrentUserMiddleware>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped(sp => (ICurrentUserInitializer)sp.GetRequiredService<ICurrentUser>());
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IRoleService, RoleService>();
        services.BindDbContext<IdentityDbContext>();
        services.AddScoped<IDbInitializer, IdentityDbInitializer>();
        services.AddIdentity<FshUser, FshRole>(options =>
        {
            options.Password.RequiredLength = IdentityModuleConstants.PasswordLength;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
        })
           .AddEntityFrameworkStores<IdentityDbContext>()
           .AddDefaultTokenProviders();
        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        TokenGenerationEndpoint.Map(endpoints);
        return endpoints;
    }
}
