using System.Collections.ObjectModel;

namespace FSH.Framework.Shared.Constants;

public static class RoleConstants
{
    public const string Admin = nameof(Admin);
    public const string Basic = nameof(Basic);

    /// <summary>
    /// The base roles provided by the framework.
    /// </summary>
    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Basic
    });

    /// <summary>
    /// Determines whether the role is a framework-defined default.
    /// </summary>
    public static bool IsDefault(string roleName) => DefaultRoles.Contains(roleName);
}