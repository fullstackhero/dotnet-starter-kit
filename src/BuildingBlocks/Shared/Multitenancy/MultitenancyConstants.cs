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

#pragma warning disable S2068 // Credentials should not be hard-coded — this is a seeding default, not a real credential
    public const string DefaultPassword = "123Pa$$word!";
#pragma warning restore S2068
    public const string Identifier = "tenant";
    public const string Schema = "tenant";

    public static class Permissions
    {
        public const string View = "Permissions.Tenants.View";
        public const string Create = "Permissions.Tenants.Create";
        public const string Update = "Permissions.Tenants.Update";
        public const string ViewTheme = "Permissions.Tenants.ViewTheme";
        public const string UpdateTheme = "Permissions.Tenants.UpdateTheme";
    }
}
