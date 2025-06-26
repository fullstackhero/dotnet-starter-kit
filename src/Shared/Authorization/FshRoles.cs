using System.Collections.ObjectModel;

namespace FSH.Starter.Shared.Authorization;

public static class FshRoles
{
    public const string Admin = nameof(Admin);
    public const string Basic = nameof(Basic);

    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Basic
    });

    public static bool IsDefault(string roleName) => DefaultRoles.Any(r => string.Equals(r, roleName, StringComparison.Ordinal));
}
