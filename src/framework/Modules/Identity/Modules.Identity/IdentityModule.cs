using Asp.Versioning;
using FluentValidation;
using FSH.Framework.Core.ExecutionContext;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Identity.Authorization;
using FSH.Framework.Identity.Core.Roles;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Identity.Core.Users;
using FSH.Framework.Identity.Infrastructure.Data;
using FSH.Framework.Identity.Infrastructure.Roles;
using FSH.Framework.Identity.Infrastructure.Tokens;
using FSH.Framework.Identity.Infrastructure.Users;
using FSH.Framework.Identity.v1.Tokens.TokenGeneration;
using FSH.Framework.Identity.v1.Users;
using FSH.Framework.Infrastructure.Auth;
using FSH.Framework.Infrastructure.Auth.Jwt;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Identity.Roles.Endpoints;
using FSH.Framework.Infrastructure.Identity.Users.Services;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Modules.Identity.Contracts;
using FSH.Modules.Common.Infrastructure.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.Modules.Identity;
public class IdentityModule : IModule
{
    public void AddModule(IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assemblies = new Assembly[]
        {
            typeof(IdentityModule).Assembly
        };
        services.RegisterCommandAndQueryHandlers(assemblies);
        var scanResults = AssemblyScanner
            .FindValidatorsInAssemblies(assemblies, true)
            .Where(r => r.ValidatorType != typeof(UserImageValidator))
            .ToList();

        foreach (var result in scanResults)
        {
            services.AddScoped(result.InterfaceType, result.ValidatorType);
        }
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
    }

    public void ConfigureModule(WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = app
            .MapGroup("api/v{version:apiVersion}/identity")
            .WithTags("Identity")
            .WithOpenApi()
            .WithApiVersionSet(apiVersionSet);

        TokenGenerationEndpoint.Map(group).AllowAnonymous();
        GetRolesEndpoint.MapGetRolesEndpoint(group);
        GetRoleByIdEndpoint.MapGetRoleEndpoint(group);
    }
}