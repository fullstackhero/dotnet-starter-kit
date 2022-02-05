namespace FSH.WebApi.Shared.Multitenancy;

public class MultitenancyConstants
{
    public static class Root
    {
        public const string Id = "root";
        public const string Name = "Root";
        public const string EmailAddress = "admin@root.com";
    }

    public const string DefaultPassword = "123Pa$$word!";

    public const string TenantIdName = "tenant";

    // Configurable and could be loaded from config file
    public static int MinimumAdmins = 2;
}