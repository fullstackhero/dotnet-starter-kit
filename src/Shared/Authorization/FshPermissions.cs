using System.Collections.ObjectModel;

namespace FSH.Starter.Shared.Authorization;

public static class FshPermissions
{
    private static readonly FshPermission[] AllPermissions =
    [     
        //tenants
        new("View Tenants", FshActions.View, FshResources.Tenants, IsRoot: true),
        new("Create Tenants", FshActions.Create, FshResources.Tenants, IsRoot: true),
        new("Update Tenants", FshActions.Update, FshResources.Tenants, IsRoot: true),
        new("Upgrade Tenant Subscription", FshActions.UpgradeSubscription, FshResources.Tenants, IsRoot: true),

        //identity
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
        
        //products
        new("View Products", FshActions.View, FshResources.Products, IsBasic: true),
        new("Search Products", FshActions.Search, FshResources.Products, IsBasic: true),
        new("Create Products", FshActions.Create, FshResources.Products),
        new("Update Products", FshActions.Update, FshResources.Products),
        new("Delete Products", FshActions.Delete, FshResources.Products),
        new("Export Products", FshActions.Export, FshResources.Products),

        //brands
        new("View Brands", FshActions.View, FshResources.Brands, IsBasic: true),
        new("Search Brands", FshActions.Search, FshResources.Brands, IsBasic: true),
        new("Create Brands", FshActions.Create, FshResources.Brands),
        new("Update Brands", FshActions.Update, FshResources.Brands),
        new("Delete Brands", FshActions.Delete, FshResources.Brands),
        new("Export Brands", FshActions.Export, FshResources.Brands),

        //todos
        new("View Todos", FshActions.View, FshResources.Todos, IsBasic: true),
        new("Search Todos", FshActions.Search, FshResources.Todos, IsBasic: true),
        new("Create Todos", FshActions.Create, FshResources.Todos),
        new("Update Todos", FshActions.Update, FshResources.Todos),
        new("Delete Todos", FshActions.Delete, FshResources.Todos),
        new("Export Todos", FshActions.Export, FshResources.Todos),

        //water - customers
        new("View Customers", FshActions.View, FshResources.Customers, IsBasic: true),
        new("Search Customers", FshActions.Search, FshResources.Customers, IsBasic: true),
        new("Create Customers", FshActions.Create, FshResources.Customers),
        new("Update Customers", FshActions.Update, FshResources.Customers),
        new("Delete Customers", FshActions.Delete, FshResources.Customers),
        new("Export Customers", FshActions.Export, FshResources.Customers),

        //water - meters
        new("View Meters", FshActions.View, FshResources.Meters, IsBasic: true),
        new("Search Meters", FshActions.Search, FshResources.Meters, IsBasic: true),
        new("Create Meters", FshActions.Create, FshResources.Meters),
        new("Update Meters", FshActions.Update, FshResources.Meters),
        new("Delete Meters", FshActions.Delete, FshResources.Meters),
        new("Export Meters", FshActions.Export, FshResources.Meters),

        //water - meter readings
        new("View Meter Readings", FshActions.View, FshResources.MeterReadings, IsBasic: true),
        new("Search Meter Readings", FshActions.Search, FshResources.MeterReadings, IsBasic: true),
        new("Create Meter Readings", FshActions.Create, FshResources.MeterReadings),
        new("Export Meter Readings", FshActions.Export, FshResources.MeterReadings),

        //water - bills
        new("View Bills", FshActions.View, FshResources.Bills, IsBasic: true),
        new("Search Bills", FshActions.Search, FshResources.Bills, IsBasic: true),
        new("Create Bills", FshActions.Create, FshResources.Bills),
        new("Export Bills", FshActions.Export, FshResources.Bills),

        //water - payments
        new("View Payments", FshActions.View, FshResources.Payments, IsBasic: true),
        new("Search Payments", FshActions.Search, FshResources.Payments, IsBasic: true),
        new("Create Payments", FshActions.Create, FshResources.Payments),
        new("Export Payments", FshActions.Export, FshResources.Payments),

        //water - tariffs
        new("View Tariffs", FshActions.View, FshResources.Tariffs, IsBasic: true),
        new("Search Tariffs", FshActions.Search, FshResources.Tariffs, IsBasic: true),
        new("Create Tariffs", FshActions.Create, FshResources.Tariffs),
        new("Update Tariffs", FshActions.Update, FshResources.Tariffs),
        new("Delete Tariffs", FshActions.Delete, FshResources.Tariffs),
        new("Export Tariffs", FshActions.Export, FshResources.Tariffs),

        //water - trouble tickets
        new("View Trouble Tickets", FshActions.View, FshResources.MeterTroubleTickets, IsBasic: true),
        new("Search Trouble Tickets", FshActions.Search, FshResources.MeterTroubleTickets, IsBasic: true),
        new("Create Trouble Tickets", FshActions.Create, FshResources.MeterTroubleTickets),
        new("Update Trouble Tickets", FshActions.Update, FshResources.MeterTroubleTickets),
        new("Delete Trouble Tickets", FshActions.Delete, FshResources.MeterTroubleTickets),
        new("Export Trouble Tickets", FshActions.Export, FshResources.MeterTroubleTickets),

         new("View Hangfire", FshActions.View, FshResources.Hangfire),
         new("View Dashboard", FshActions.View, FshResources.Dashboard),

        //audit
        new("View Audit Trails", FshActions.View, FshResources.AuditTrails),
    ];

    public static IReadOnlyList<FshPermission> All { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions);
    public static IReadOnlyList<FshPermission> Root { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions.Where(p => p.IsRoot).ToArray());
    public static IReadOnlyList<FshPermission> Admin { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions.Where(p => !p.IsRoot).ToArray());
    public static IReadOnlyList<FshPermission> Basic { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions.Where(p => p.IsBasic).ToArray());
}

public record FshPermission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    public string Name => NameFor(Action, Resource);
    public static string NameFor(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }
}


