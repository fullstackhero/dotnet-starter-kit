namespace FSH.WebApi.Shared.Authorization;

public class FSHRootPermissions
{
    public static class Tenants
    {
        public const string View = "Permissions.Root.Tenants.View";
        public const string ListAll = "Permissions.Root.Tenants.ViewAll";
        public const string Create = "Permissions.Root.Tenants.Register";
        public const string Update = "Permissions.Root.Tenants.Update";
        public const string UpgradeSubscription = "Permissions.Root.Tenants.UpgradeSubscription";
        public const string Remove = "Permissions.Root.Tenants.Remove";
    }
}