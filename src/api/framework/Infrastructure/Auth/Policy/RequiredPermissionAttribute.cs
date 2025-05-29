namespace FSH.Framework.Infrastructure.Auth.Policy;

public interface IRequiredPermissionMetadata
{
    HashSet<string> RequiredPermissions { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiredPermissionAttribute(string? requiredPermission, params string[]? additionalRequiredPermissions)
    : Attribute, IRequiredPermissionMetadata
{
    public HashSet<string> RequiredPermissions { get; } = CreatePermissionSet(requiredPermission, additionalRequiredPermissions);

    public string? RequiredPermission { get; }

    public string[]? AdditionalRequiredPermissions { get; }

    private static HashSet<string> CreatePermissionSet(string? requiredPermission, string[]? additionalRequiredPermissions)
    {
        var permissions = new HashSet<string>();
        
        if (!string.IsNullOrEmpty(requiredPermission))
        {
            permissions.Add(requiredPermission);
        }
        
        if (additionalRequiredPermissions != null)
        {
            foreach (var permission in additionalRequiredPermissions)
            {
                if (!string.IsNullOrEmpty(permission))
                {
                    permissions.Add(permission);
                }
            }
        }
        
        return permissions;
    }
}
