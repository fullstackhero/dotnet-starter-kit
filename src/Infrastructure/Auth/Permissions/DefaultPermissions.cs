using FSH.WebApi.Infrastructure.Common.Extensions;
using FSH.WebApi.Shared.Authorization;

namespace FSH.WebApi.Infrastructure.Auth.Permissions;

public static class DefaultPermissions
{
    public static List<string> Basic => new()
    {
        FSHPermissions.Products.Search,
        FSHPermissions.Products.View,
        FSHPermissions.Brands.Search,
        FSHPermissions.Brands.View,
        FSHPermissions.RoleClaims.View
    };

    public static List<string> Admin => typeof(FSHPermissions).GetNestedClassesStaticStringValues();

    public static List<string> Root => typeof(FSHRootPermissions).GetNestedClassesStaticStringValues();
}