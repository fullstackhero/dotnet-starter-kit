using FSH.WebAPI.Shared.Authorization;

namespace FSH.WebAPI.Infrastructure.Auth.Permissions;

public static class DefaultPermissions
{
    public static List<string> Basics => new()
    {
        FSHPermissions.Products.Search,
        FSHPermissions.Products.View,
        FSHPermissions.Brands.Search,
        FSHPermissions.Brands.View
    };
}