namespace FSH.Framework.Shared.Multitenancy;
public static class MultiTenancyConstants
{
    public static class Root
    {
        public const string Id = "root";
        public const string Name = "Root";
        public const string EmailAddress = "admin@root.com";
        public const string DefaultProfilePicture = "assets/defaults/profile-picture.webp";
    }

    public const string DefaultPassword = "123Pa$$word!";

    public const string Identifier = "tenant";
}