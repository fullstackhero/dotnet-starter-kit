using Microsoft.AspNetCore.Authorization;

namespace FSH.Framework.Infrastructure.Auth.Permissions;

public sealed class MustHavePermissionAttribute : AuthorizeAttribute
{
    public MustHavePermissionAttribute(string? action, string? resource)
    {
        Policy = FshPermission.NameFor(action, resource);
    }

    public string? Action { get; }
    public string? Resource { get; }
}
