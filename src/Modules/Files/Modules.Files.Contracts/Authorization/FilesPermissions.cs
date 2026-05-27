using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Files.Contracts.Authorization;

/// <summary>
/// Permission constants + the registry entry consumed by <c>FilesModule.ConfigureServices</c>.
/// Permission names follow the <c>Permissions.{Resource}.{Action}</c> shape per the framework
/// convention (see <see cref="FshPermission.NameFor"/>).
/// </summary>
public static class FilesPermissions
{
    public const string Resource = "Files";

    public const string Upload    = $"Permissions.{Resource}.Upload";
    public const string DeleteOwn = $"Permissions.{Resource}.DeleteOwn";
    public const string DeleteAny = $"Permissions.{Resource}.DeleteAny";
    public const string ViewTrash = $"Permissions.{Resource}.ViewTrash";
    public const string Restore   = $"Permissions.{Resource}.Restore";

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("Upload Files",      "Upload",    Resource, IsBasic: true),
        new("Delete Own Files",  "DeleteOwn", Resource, IsBasic: true),
        new("Delete Any File",   "DeleteAny", Resource),
        new("View Files Trash",  "ViewTrash", Resource),
        new("Restore Files",     "Restore",   Resource),
    ];
}
