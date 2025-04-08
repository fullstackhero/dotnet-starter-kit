using Microsoft.AspNetCore.Builder;

namespace FSH.Framework.Infrastructure.Auth.Policy;
public static class EndpointExtensions
{
    public static TBuilder RequirePermission<TBuilder>(
    this TBuilder endpointConventionBuilder, string requiredPermission, params string[] additionalRequiredPermissions)
    where TBuilder : IEndpointConventionBuilder
    {
        return endpointConventionBuilder.WithMetadata(new RequiredPermissionAttribute(requiredPermission, additionalRequiredPermissions));
    }
}
