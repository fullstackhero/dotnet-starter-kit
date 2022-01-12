namespace FSH.WebApi.Shared.Multitenancy;

public class MultitenancyConstants
{
    public static class Root
    {
        public const string Name = "Root";
        public const string Key = "root";
        public const string EmailAddress = "admin@root.com";
    }

    public const string DefaultPassword = "123Pa$$word!";

    public const string TenantKeyName = "tenant";
}