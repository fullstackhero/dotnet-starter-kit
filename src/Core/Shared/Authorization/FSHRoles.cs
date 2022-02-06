using System.Collections.ObjectModel;

namespace FSH.WebApi.Shared.Authorization;

public static class FSHRoles
{
    public static string Admin = nameof(Admin);
    public static string Basic = nameof(Basic);

    public static IReadOnlyList<string> Default { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Basic
    });

    public static bool IsDefault(string roleName) => Default.Any(r => r == roleName);
}