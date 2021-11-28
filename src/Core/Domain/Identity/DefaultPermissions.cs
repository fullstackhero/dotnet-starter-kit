namespace DN.WebApi.Domain.Constants;

public static class DefaultPermissions
{
    public static List<string> Basics => new()
    {
        PermissionConstants.Products.Search,
        PermissionConstants.Products.View,
        PermissionConstants.Brands.Search,
        PermissionConstants.Brands.View
    };
}