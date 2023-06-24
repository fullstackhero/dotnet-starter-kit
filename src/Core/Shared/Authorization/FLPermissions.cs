using System.Collections.ObjectModel;

namespace FL_CRMS_ERP_WEBAPI.Shared.Authorization;

public static class FLAction
{
    public const string View = nameof(View);
    public const string Search = nameof(Search);
    public const string Create = nameof(Create);
    public const string Update = nameof(Update);
    public const string Delete = nameof(Delete);
    public const string Export = nameof(Export);
    public const string Generate = nameof(Generate);
    public const string Clean = nameof(Clean);
    public const string UpgradeSubscription = nameof(UpgradeSubscription);
}

public static class FLResource
{
    public const string Tenants = nameof(Tenants);
    public const string Dashboard = nameof(Dashboard);
    public const string Hangfire = nameof(Hangfire);
    public const string Users = nameof(Users);
    public const string UserRoles = nameof(UserRoles);
    public const string Roles = nameof(Roles);
    public const string RoleClaims = nameof(RoleClaims);
    public const string Products = nameof(Products);
    public const string Brands = nameof(Brands);
}

public static class FLPermissions
{
    private static readonly FLPermission[] _all = new FLPermission[]
    {
        new("View Dashboard", FLAction.View, FLResource.Dashboard),
        new("View Hangfire", FLAction.View, FLResource.Hangfire),
        new("View Users", FLAction.View, FLResource.Users),
        new("Search Users", FLAction.Search, FLResource.Users),
        new("Create Users", FLAction.Create, FLResource.Users),
        new("Update Users", FLAction.Update, FLResource.Users),
        new("Delete Users", FLAction.Delete, FLResource.Users),
        new("Export Users", FLAction.Export, FLResource.Users),
        new("View UserRoles", FLAction.View, FLResource.UserRoles),
        new("Update UserRoles", FLAction.Update, FLResource.UserRoles),
        new("View Roles", FLAction.View, FLResource.Roles),
        new("Create Roles", FLAction.Create, FLResource.Roles),
        new("Update Roles", FLAction.Update, FLResource.Roles),
        new("Delete Roles", FLAction.Delete, FLResource.Roles),
        new("View RoleClaims", FLAction.View, FLResource.RoleClaims),
        new("Update RoleClaims", FLAction.Update, FLResource.RoleClaims),
        new("View Products", FLAction.View, FLResource.Products, IsBasic: true),
        new("Search Products", FLAction.Search, FLResource.Products, IsBasic: true),
        new("Create Products", FLAction.Create, FLResource.Products),
        new("Update Products", FLAction.Update, FLResource.Products),
        new("Delete Products", FLAction.Delete, FLResource.Products),
        new("Export Products", FLAction.Export, FLResource.Products),
        new("View Brands", FLAction.View, FLResource.Brands, IsBasic: true),
        new("Search Brands", FLAction.Search, FLResource.Brands, IsBasic: true),
        new("Create Brands", FLAction.Create, FLResource.Brands),
        new("Update Brands", FLAction.Update, FLResource.Brands),
        new("Delete Brands", FLAction.Delete, FLResource.Brands),
        new("Generate Brands", FLAction.Generate, FLResource.Brands),
        new("Clean Brands", FLAction.Clean, FLResource.Brands),
        new("View Tenants", FLAction.View, FLResource.Tenants, IsRoot: true),
        new("Create Tenants", FLAction.Create, FLResource.Tenants, IsRoot: true),
        new("Update Tenants", FLAction.Update, FLResource.Tenants, IsRoot: true),
        new("Upgrade Tenant Subscription", FLAction.UpgradeSubscription, FLResource.Tenants, IsRoot: true)
    };

    public static IReadOnlyList<FLPermission> All { get; } = new ReadOnlyCollection<FLPermission>(_all);
    public static IReadOnlyList<FLPermission> Root { get; } = new ReadOnlyCollection<FLPermission>(_all.Where(p => p.IsRoot).ToArray());
    public static IReadOnlyList<FLPermission> Admin { get; } = new ReadOnlyCollection<FLPermission>(_all.Where(p => !p.IsRoot).ToArray());
    public static IReadOnlyList<FLPermission> Basic { get; } = new ReadOnlyCollection<FLPermission>(_all.Where(p => p.IsBasic).ToArray());
}

public record FLPermission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    public string Name => NameFor(Action, Resource);
    public static string NameFor(string action, string resource) => $"Permissions.{resource}.{action}";
}
