using FSH.WebApi.Infrastructure.Common.Extensions;
using FSH.WebApi.Shared.Authorization;

namespace FSH.WebApi.Infrastructure.Auth.Permissions;

public static class DefaultPermissions
{
    public static Type[] AdminPermissionTypes => typeof(FSHPermissions).GetNestedTypes();
    public static Type[] RootPermissionTypes => typeof(FSHRootPermissions).GetNestedTypes();
    public static Type[] BasicPermissionTypes => typeof(FSHBasicPermissions).GetNestedTypes();
}