using System;
using System.Collections.Generic;
using System.Linq;
namespace FSH.Framework.Shared.Identity.Authorization;

public interface IRequiredPermissionMetadata
{
    HashSet<string> RequiredPermissions { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiredPermissionAttribute : Attribute, IRequiredPermissionMetadata
{
    public HashSet<string> RequiredPermissions { get; }
    public string? RequiredPermission { get; }
    public string[]? AdditionalRequiredPermissions { get; }

    public RequiredPermissionAttribute(string? requiredPermission, params string[]? additionalRequiredPermissions)
    {
        RequiredPermission = requiredPermission;
        AdditionalRequiredPermissions = additionalRequiredPermissions;

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(requiredPermission))
        {
            permissions.Add(requiredPermission);
        }

        if (additionalRequiredPermissions is { Length: > 0 })
        {
            foreach (var p in additionalRequiredPermissions.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                permissions.Add(p);
            }
        }

        RequiredPermissions = permissions;
    }
}