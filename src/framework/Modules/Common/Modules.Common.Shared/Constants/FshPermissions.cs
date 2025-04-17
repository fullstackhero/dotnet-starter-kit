namespace FSH.Framework.Shared.Constants;

public static class FshPermissions
{
    private static readonly List<FshPermission> _all = new()
    {
        // Built-in permissions

        // Tenants
        new("View Tenants", FshActions.View, FshResources.Tenants, IsRoot: true),
        new("Create Tenants", FshActions.Create, FshResources.Tenants, IsRoot: true),
        new("Update Tenants", FshActions.Update, FshResources.Tenants, IsRoot: true),
        new("Upgrade Tenant Subscription", FshActions.UpgradeSubscription, FshResources.Tenants, IsRoot: true),

        // Identity
        new("View Users", FshActions.View, FshResources.Users),
        new("Search Users", FshActions.Search, FshResources.Users),
        new("Create Users", FshActions.Create, FshResources.Users),
        new("Update Users", FshActions.Update, FshResources.Users),
        new("Delete Users", FshActions.Delete, FshResources.Users),
        new("Export Users", FshActions.Export, FshResources.Users),
        new("View UserRoles", FshActions.View, FshResources.UserRoles),
        new("Update UserRoles", FshActions.Update, FshResources.UserRoles),
        new("View Roles", FshActions.View, FshResources.Roles),
        new("Create Roles", FshActions.Create, FshResources.Roles),
        new("Update Roles", FshActions.Update, FshResources.Roles),
        new("Delete Roles", FshActions.Delete, FshResources.Roles),
        new("View RoleClaims", FshActions.View, FshResources.RoleClaims),
        new("Update RoleClaims", FshActions.Update, FshResources.RoleClaims),

        // Audit
        new("View Audit Trails", FshActions.View, FshResources.AuditTrails),

        // Hangfire / Dashboard
        new("View Hangfire", FshActions.View, FshResources.Hangfire),
        new("View Dashboard", FshActions.View, FshResources.Dashboard),
    };

    /// <summary>
    /// Register additional permissions from external projects/modules.
    /// </summary>
    public static void Register(IEnumerable<FshPermission> additionalPermissions)
    {
        foreach (var permission in additionalPermissions)
        {
            if (!_all.Any(p => p.Name == permission.Name))
                _all.Add(permission);
        }
    }

    public static IReadOnlyList<FshPermission> All => _all.AsReadOnly();
    public static IReadOnlyList<FshPermission> Root => _all.Where(p => p.IsRoot).ToList();
    public static IReadOnlyList<FshPermission> Admin => _all.Where(p => !p.IsRoot).ToList();
    public static IReadOnlyList<FshPermission> Basic => _all.Where(p => p.IsBasic).ToList();
}

public record FshPermission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    public string Name => NameFor(Action, Resource);
    public static string NameFor(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }
}