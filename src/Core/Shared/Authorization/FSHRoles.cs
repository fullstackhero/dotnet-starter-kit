using System.Collections.ObjectModel;

namespace FSH.WebApi.Shared.Authorization;

public static class FSHRoles
{
    public static string Admin = nameof(Admin);
    public static string Basic = nameof(Basic);

    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Basic
    });

    public static bool IsDefault(string roleName) => DefaultRoles.Any(r => r == roleName);
}