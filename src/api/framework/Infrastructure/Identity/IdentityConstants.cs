using System.Collections.ObjectModel;

namespace FSH.Framework.Infrastructure.Identity;
internal static class IdentityConstants
{
    public const int PasswordLength = 6;
    public const string SchemaName = "identity";
    public const string RootTenant = "root";
    public const string DefaultPassword = "123Pa$$word!";
    public const string DefaultProfilePicture = "assets/defaults/profile-picture.webp";

    public static class Roles
    {
        public const string Admin = nameof(Admin);
        public const string Basic = nameof(Basic);
        public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
        {
            Admin,
            Basic
        });
    }
    public static class Claims
    {
        public const string Tenant = "tenant";
        public const string Fullname = "fullName";
        public const string Permission = "permission";
        public const string ImageUrl = "image_url";
        public const string IpAddress = "ipAddress";
        public const string Expiration = "exp";
    }
}
