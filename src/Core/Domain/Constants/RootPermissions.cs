namespace DN.WebApi.Domain.Constants;

public class RootPermissions
{
    public static class Tenants
    {
        public const string View = "Permissions.Tenants.View";
        public const string ListAll = "Permissions.Tenants.ViewAll";
        public const string Create = "Permissions.Tenants.Register";
        public const string Update = "Permissions.Tenants.Update";
        public const string UpgradeSubscription = "Permissions.Tenants.UpgradeSubscription";
        public const string Remove = "Permissions.Tenants.Remove";
    }
}