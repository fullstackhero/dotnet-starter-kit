using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FSH.Modules.Identity.Authorization;

public static class RequiredPermissionDefaults
{
    public const string PolicyName = "RequiredPermission";
}

public static class RequiredPermissionAuthorizationExtensions
{
    public static AuthorizationPolicyBuilder RequireRequiredPermissions(this AuthorizationPolicyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddRequirements(new PermissionAuthorizationRequirement());
    }

    public static AuthorizationBuilder AddRequiredPermissionPolicy(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy(RequiredPermissionDefaults.PolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddAuthenticationSchemes(AuthenticationConstants.AuthenticationScheme);
            policy.RequireRequiredPermissions();
        });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, RequiredPermissionAuthorizationHandler>());

        return builder;
    }
}