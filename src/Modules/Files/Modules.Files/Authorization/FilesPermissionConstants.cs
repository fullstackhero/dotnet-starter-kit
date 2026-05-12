namespace FSH.Modules.Files.Authorization;

/// <summary>Permission strings claimed by the Files module. Registered via <c>PermissionConstants.Register</c> in <c>FilesModule.ConfigureServices</c>.</summary>
public static class FilesPermissionConstants
{
    public const string Upload    = "Permissions.Files.Upload";
    public const string DeleteOwn = "Permissions.Files.DeleteOwn";
    public const string DeleteAny = "Permissions.Files.DeleteAny";
    public const string ViewTrash = "Permissions.Files.ViewTrash";
    public const string Restore   = "Permissions.Files.Restore";
}

internal static class FilesPermissions
{
    public static readonly string[] All =
    [
        FilesPermissionConstants.Upload,
        FilesPermissionConstants.DeleteOwn,
        FilesPermissionConstants.DeleteAny,
        FilesPermissionConstants.ViewTrash,
        FilesPermissionConstants.Restore
    ];
}
