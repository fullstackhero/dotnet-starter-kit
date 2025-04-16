using Asp.Versioning;
using FSH.Framework.Core.ExecutionContext;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Identity.Authorization;
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
using FSH.Framework.Infrastructure.Auth.Jwt;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Identity.Users.Services;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.Framework.Identity;
public static class IdentityModule
{
    public static IServiceCollection RegisterIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assemblies = new Assembly[]
       {
            typeof(IdentityModule).Assembly,
            typeof(IdentityModuleConstants).Assembly
       };

        services.RegisterCommandAndQueryHandlers(assemblies);
        services.AddScoped<CurrentUserMiddleware>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PathAwareAuthorizationHandler>();
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
        services.ConfigureJwtAuth();
        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/identity")
            .WithTags("Identity")
            .WithOpenApi()
            .WithApiVersionSet(apiVersionSet);

        TokenGenerationEndpoint.Map(group);
        return endpoints;
    }
}
