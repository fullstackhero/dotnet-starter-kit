namespace FSH.Framework.Shared.Multitenancy;

public static class MultitenancyConstants
{
    public static class Root
    {
        public const string Id = "root";
        public const string Name = "Root";
        public const string EmailAddress = "admin@root.com";
        public const string DefaultProfilePicture = "assets/defaults/profile-picture.webp";
        public const string Issuer = "mukesh.murugan";
    }

    public const string Identifier = "tenant";
    public const string Schema = "tenant";
}