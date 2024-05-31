using System.Collections.ObjectModel;

namespace FSH.Framework.Infrastructure.Auth;
internal static class AuthConstants
{
    internal static class Claims
    {
        public const string Tenant = "tenant";
        public const string Fullname = "fullName";
        public const string Permission = "permission";
        public const string ImageUrl = "image_url";
        public const string IpAddress = "ipAddress";
        public const string Expiration = "exp";
    }
    public static class Roles
    {
        public const string Admin = nameof(Admin);
        public const string Basic = nameof(Basic);

        public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
        {
            Admin,
            Basic
        });

        public static bool IsDefault(string roleName) => DefaultRoles.Any(r => r == roleName);
    }
}
