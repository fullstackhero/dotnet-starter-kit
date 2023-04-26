using System.Collections.ObjectModel;

namespace FSH.WebApi.Shared.Authorization;

public static class FSHAction
{
    public const string View = nameof(View);
    public const string Search = nameof(Search);
    public const string Create = nameof(Create);
    public const string Update = nameof(Update);
    public const string Delete = nameof(Delete);
    public const string Export = nameof(Export);
    public const string Import = nameof(Import);
    public const string Clean = nameof(Clean);

    public const string Generate = nameof(Generate);
    public const string UpgradeSubscription = nameof(UpgradeSubscription);
}

public static class FSHResource
{
    public const string Menus = nameof(Menus);

    public const string Dashboard = nameof(Dashboard);
    public const string Hangfire = nameof(Hangfire);

    public const string Tenants = nameof(Tenants);
    public const string Users = nameof(Users);
    public const string UserRoles = nameof(UserRoles);
    public const string Roles = nameof(Roles);
    public const string RoleClaims = nameof(RoleClaims);

    public const string GeoAdminUnits = nameof(GeoAdminUnits);
    public const string Countries = nameof(Countries);
    public const string States = nameof(States);
    public const string Regions = nameof(Regions);
    public const string Provinces = nameof(Provinces);
    public const string Districts = nameof(Districts);
    public const string Wards = nameof(Wards);

    public const string BusinessUnits = nameof(BusinessUnits);
    public const string Departments = nameof(Departments);
    public const string SubDepartments = nameof(SubDepartments);
    public const string Teams = nameof(Teams);

    public const string Employees = nameof(Employees);
    public const string Titles = nameof(Titles);

    public const string Quizs = nameof(Quizs);
    public const string QuizResults = nameof(QuizResults);

    public const string Vendors = nameof(Vendors);

    public const string Brands = nameof(Brands);
    public const string BusinessLines = nameof(BusinessLines);
    public const string GroupCategories = nameof(GroupCategories);
    public const string Categories = nameof(Categories);
    public const string SubCategories = nameof(SubCategories);

    public const string Assets = nameof(Assets);
    public const string AssetStatuses = nameof(AssetStatuses);
    public const string AssetHistorys = nameof(AssetHistorys);

    public const string Channels = nameof(Channels);
    public const string Retailers = nameof(Retailers);
    public const string Stores = nameof(Stores);

    public const string PriceGroups = nameof(PriceGroups);
    public const string PricePlans = nameof(PricePlans);

    public const string Products = nameof(Products);
}

public static class FSHPermissions
{
    private static readonly FSHPermission[] _all = new FSHPermission[]
    {
        #region General UI
        new("View Menus", FSHAction.View, FSHResource.Menus),
        new("Search Menus", FSHAction.Search, FSHResource.Menus),
        new("Create Menus", FSHAction.Create, FSHResource.Menus),
        new("Update Menus", FSHAction.Update, FSHResource.Menus),
        new("Delete Menus", FSHAction.Delete, FSHResource.Menus),
        new("Export Menus", FSHAction.Export, FSHResource.Menus),
        new("Import Menus", FSHAction.Import, FSHResource.Menus),

        new("View Dashboard", FSHAction.View, FSHResource.Dashboard),
        new("View Hangfire", FSHAction.View, FSHResource.Hangfire),

        #endregion

        #region Users
        new("View Users", FSHAction.View, FSHResource.Users),
        new("Search Users", FSHAction.Search, FSHResource.Users),
        new("Create Users", FSHAction.Create, FSHResource.Users),
        new("Update Users", FSHAction.Update, FSHResource.Users),
        new("Delete Users", FSHAction.Delete, FSHResource.Users),
        new("Export Users", FSHAction.Export, FSHResource.Users),
        new("Import Users", FSHAction.Import, FSHResource.Users),

        new("View UserRoles", FSHAction.View, FSHResource.UserRoles),
        new("Update UserRoles", FSHAction.Update, FSHResource.UserRoles),

        new("View Roles", FSHAction.View, FSHResource.Roles),
        new("Search Roles", FSHAction.Search, FSHResource.Roles),
        new("Create Roles", FSHAction.Create, FSHResource.Roles),
        new("Update Roles", FSHAction.Update, FSHResource.Roles),
        new("Delete Roles", FSHAction.Delete, FSHResource.Roles),
        new("Export Roles", FSHAction.Export, FSHResource.Roles),
        new("Import Roles", FSHAction.Import, FSHResource.Roles),

        new("View RoleClaims", FSHAction.View, FSHResource.RoleClaims),
        new("Update RoleClaims", FSHAction.Update, FSHResource.RoleClaims),

        #endregion

        #region Tenant
        new("Upgrade Tenant Subscription", FSHAction.UpgradeSubscription, FSHResource.Tenants, IsRoot: true),

        new("View Tenants", FSHAction.View, FSHResource.Tenants, IsRoot: true),
        new("Search Tenants", FSHAction.Search, FSHResource.Tenants, IsRoot: true),
        new("Create Tenants", FSHAction.Create, FSHResource.Tenants, IsRoot: true),
        new("Update Tenants", FSHAction.Update, FSHResource.Tenants, IsRoot: true),

        new("Delete Tenants", FSHAction.Delete, FSHResource.Tenants, IsRoot: true),
        new("Export Tenants", FSHAction.Export, FSHResource.Tenants, IsRoot: true),
        new("Import Tenants", FSHAction.Import, FSHResource.Tenants, IsRoot: true),

        #endregion

        #region Geo
        new("View GeoAdminUnits", FSHAction.View, FSHResource.GeoAdminUnits),
        new("Search GeoAdminUnits", FSHAction.Search, FSHResource.GeoAdminUnits),
        new("Create GeoAdminUnits", FSHAction.Create, FSHResource.GeoAdminUnits),
        new("Update GeoAdminUnits", FSHAction.Update, FSHResource.GeoAdminUnits),
        new("Delete GeoAdminUnits", FSHAction.Delete, FSHResource.GeoAdminUnits),
        new("Export GeoAdminUnits", FSHAction.Export, FSHResource.GeoAdminUnits),
        new("Import GeoAdminUnits", FSHAction.Import, FSHResource.GeoAdminUnits),

        new("View Countries", FSHAction.View, FSHResource.Countries),
        new("Search Countries", FSHAction.Search, FSHResource.Countries),
        new("Create Countries", FSHAction.Create, FSHResource.Countries),
        new("Update Countries", FSHAction.Update, FSHResource.Countries),
        new("Delete Countries", FSHAction.Delete, FSHResource.Countries),
        new("Export Countries", FSHAction.Export, FSHResource.Countries),
        new("Import Countries", FSHAction.Import, FSHResource.Countries),

        new("View States", FSHAction.View, FSHResource.States),
        new("Search States", FSHAction.Search, FSHResource.States),
        new("Create States", FSHAction.Create, FSHResource.States),
        new("Update States", FSHAction.Update, FSHResource.States),
        new("Delete States", FSHAction.Delete, FSHResource.States),
        new("Export States", FSHAction.Export, FSHResource.States),
        new("Import States", FSHAction.Import, FSHResource.States),

        new("View Regions", FSHAction.View, FSHResource.Regions),
        new("Search Regions", FSHAction.Search, FSHResource.Regions),
        new("Create Regions", FSHAction.Create, FSHResource.Regions),
        new("Update Regions", FSHAction.Update, FSHResource.Regions),
        new("Delete Regions", FSHAction.Delete, FSHResource.Regions),
        new("Export Regions", FSHAction.Export, FSHResource.Regions),
        new("Import Regions", FSHAction.Import, FSHResource.Regions),

        new("View Provinces", FSHAction.View, FSHResource.Provinces),
        new("Search Provinces", FSHAction.Search, FSHResource.Provinces),
        new("Create Provinces", FSHAction.Create, FSHResource.Provinces),
        new("Update Provinces", FSHAction.Update, FSHResource.Provinces),
        new("Delete Provinces", FSHAction.Delete, FSHResource.Provinces),
        new("Export Provinces", FSHAction.Export, FSHResource.Provinces),
        new("Import Provinces", FSHAction.Import, FSHResource.Provinces),

        new("View Districts", FSHAction.View, FSHResource.Districts),
        new("Search Districts", FSHAction.Search, FSHResource.Districts),
        new("Create Districts", FSHAction.Create, FSHResource.Districts),
        new("Update Districts", FSHAction.Update, FSHResource.Districts),
        new("Delete Districts", FSHAction.Delete, FSHResource.Districts),
        new("Export Districts", FSHAction.Export, FSHResource.Districts),
        new("Import Districts", FSHAction.Import, FSHResource.Districts),

        new("View Wards", FSHAction.View, FSHResource.Wards),
        new("Search Wards", FSHAction.Search, FSHResource.Wards),
        new("Create Wards", FSHAction.Create, FSHResource.Wards),
        new("Update Wards", FSHAction.Update, FSHResource.Wards),
        new("Delete Wards", FSHAction.Delete, FSHResource.Wards),
        new("Export Wards", FSHAction.Export, FSHResource.Wards),
        new("Import Wards", FSHAction.Import, FSHResource.Wards),

        #endregion

        #region Organization
        new("View BusinessUnits", FSHAction.View, FSHResource.BusinessUnits),
        new("Search BusinessUnits", FSHAction.Search, FSHResource.BusinessUnits),
        new("Create BusinessUnits", FSHAction.Create, FSHResource.BusinessUnits),
        new("Update BusinessUnits", FSHAction.Update, FSHResource.BusinessUnits),
        new("Delete BusinessUnits", FSHAction.Delete, FSHResource.BusinessUnits),
        new("Export BusinessUnits", FSHAction.Export, FSHResource.BusinessUnits),
        new("Import BusinessUnits", FSHAction.Import, FSHResource.BusinessUnits),

        new("View Departments", FSHAction.View, FSHResource.Departments),
        new("Search Departments", FSHAction.Search, FSHResource.Departments),
        new("Create Departments", FSHAction.Create, FSHResource.Departments),
        new("Update Departments", FSHAction.Update, FSHResource.Departments),
        new("Delete Departments", FSHAction.Delete, FSHResource.Departments),
        new("Export Departments", FSHAction.Export, FSHResource.Departments),
        new("Import Departments", FSHAction.Import, FSHResource.Departments),

        new("View SubDepartments", FSHAction.View, FSHResource.SubDepartments),
        new("Search SubDepartments", FSHAction.Search, FSHResource.SubDepartments),
        new("Create SubDepartments", FSHAction.Create, FSHResource.SubDepartments),
        new("Update SubDepartments", FSHAction.Update, FSHResource.SubDepartments),
        new("Delete SubDepartments", FSHAction.Delete, FSHResource.SubDepartments),
        new("Export SubDepartments", FSHAction.Export, FSHResource.SubDepartments),
        new("Import SubDepartments", FSHAction.Import, FSHResource.SubDepartments),

        new("View Teams", FSHAction.View, FSHResource.Teams),
        new("Search Teams", FSHAction.Search, FSHResource.Teams),
        new("Create Teams", FSHAction.Create, FSHResource.Teams),
        new("Update Teams", FSHAction.Update, FSHResource.Teams),
        new("Delete Teams", FSHAction.Delete, FSHResource.Teams),
        new("Export Teams", FSHAction.Export, FSHResource.Teams),
        new("Import Teams", FSHAction.Import, FSHResource.Teams),

        #endregion

        #region People
        new("View Employees", FSHAction.View, FSHResource.Employees),
        new("Search Employees", FSHAction.Search, FSHResource.Employees),
        new("Create Employees", FSHAction.Create, FSHResource.Employees),
        new("Update Employees", FSHAction.Update, FSHResource.Employees),
        new("Delete Employees", FSHAction.Delete, FSHResource.Employees),
        new("Export Employees", FSHAction.Export, FSHResource.Employees),
        new("Import Employees", FSHAction.Import, FSHResource.Employees),

        new("View Titles", FSHAction.View, FSHResource.Titles),
        new("Search Titles", FSHAction.Search, FSHResource.Titles),
        new("Create Titles", FSHAction.Create, FSHResource.Titles),
        new("Update Titles", FSHAction.Update, FSHResource.Titles),
        new("Delete Titles", FSHAction.Delete, FSHResource.Titles),
        new("Export Titles", FSHAction.Export, FSHResource.Titles),
        new("Import Titles", FSHAction.Import, FSHResource.Titles),

        #endregion

        #region Elearning
        new("View Quizs", FSHAction.View, FSHResource.Quizs),
        new("Search Quizs", FSHAction.Search, FSHResource.Quizs),
        new("Create Quizs", FSHAction.Create, FSHResource.Quizs),
        new("Update Quizs", FSHAction.Update, FSHResource.Quizs),
        new("Delete Quizs", FSHAction.Delete, FSHResource.Quizs),
        new("Export Quizs", FSHAction.Export, FSHResource.Quizs),
        new("Import Quizs", FSHAction.Import, FSHResource.Quizs),

        new("View QuizResults", FSHAction.View, FSHResource.QuizResults),
        new("Search QuizResults", FSHAction.Search, FSHResource.QuizResults),
        new("Create QuizResults", FSHAction.Create, FSHResource.QuizResults),
        new("Update QuizResults", FSHAction.Update, FSHResource.QuizResults),
        new("Delete QuizResults", FSHAction.Delete, FSHResource.QuizResults),
        new("Export QuizResults", FSHAction.Export, FSHResource.QuizResults),
        new("Import QuizResults", FSHAction.Import, FSHResource.QuizResults),

        #endregion

        #region Purchase
        new("View Vendors", FSHAction.View, FSHResource.Vendors),
        new("Search Vendors", FSHAction.Search, FSHResource.Vendors),
        new("Create Vendors", FSHAction.Create, FSHResource.Vendors),
        new("Update Vendors", FSHAction.Update, FSHResource.Vendors),
        new("Delete Vendors", FSHAction.Delete, FSHResource.Vendors),
        new("Export Vendors", FSHAction.Export, FSHResource.Vendors),
        new("Import Vendors", FSHAction.Import, FSHResource.Vendors),

        #endregion

        #region Place
        new("View Channels", FSHAction.View, FSHResource.Channels),
        new("Search Channels", FSHAction.Search, FSHResource.Channels),
        new("Create Channels", FSHAction.Create, FSHResource.Channels),
        new("Update Channels", FSHAction.Update, FSHResource.Channels),
        new("Delete Channels", FSHAction.Delete, FSHResource.Channels),
        new("Export Channels", FSHAction.Export, FSHResource.Channels),
        new("Import Channels", FSHAction.Import, FSHResource.Channels),

        new("View Retailers", FSHAction.View, FSHResource.Retailers),
        new("Search Retailers", FSHAction.Search, FSHResource.Retailers),
        new("Create Retailers", FSHAction.Create, FSHResource.Retailers),
        new("Update Retailers", FSHAction.Update, FSHResource.Retailers),
        new("Delete Retailers", FSHAction.Delete, FSHResource.Retailers),
        new("Export Retailers", FSHAction.Export, FSHResource.Retailers),
        new("Import Retailers", FSHAction.Import, FSHResource.Retailers),

        new("View Stores", FSHAction.View, FSHResource.Stores),
        new("Search Stores", FSHAction.Search, FSHResource.Stores),
        new("Create Stores", FSHAction.Create, FSHResource.Stores),
        new("Update Stores", FSHAction.Update, FSHResource.Stores),
        new("Delete Stores", FSHAction.Delete, FSHResource.Stores),
        new("Export Stores", FSHAction.Export, FSHResource.Stores),
        new("Import Stores", FSHAction.Import, FSHResource.Stores),

        #endregion

        #region Catalog
        new ("View Brands", FSHAction.View, FSHResource.Brands, IsBasic: true),
        new("Search Brands", FSHAction.Search, FSHResource.Brands, IsBasic: true),
        new("Create Brands", FSHAction.Create, FSHResource.Brands),
        new("Update Brands", FSHAction.Update, FSHResource.Brands),
        new("Delete Brands", FSHAction.Delete, FSHResource.Brands),
        new("Export Brands", FSHAction.Export, FSHResource.Brands),
        new("Import Brands", FSHAction.Import, FSHResource.Brands),
        new("Clean Brands", FSHAction.Clean, FSHResource.Brands),

        new("Generate Brands", FSHAction.Generate, FSHResource.Brands),

        new("View BusinessLines", FSHAction.View, FSHResource.BusinessLines, IsBasic: true),
        new("Search BusinessLines", FSHAction.Search, FSHResource.BusinessLines, IsBasic: true),
        new("Create BusinessLines", FSHAction.Create, FSHResource.BusinessLines),
        new("Update BusinessLines", FSHAction.Update, FSHResource.BusinessLines),
        new("Delete BusinessLines", FSHAction.Delete, FSHResource.BusinessLines),
        new("Export BusinessLines", FSHAction.Export, FSHResource.BusinessLines),
        new("Import BusinessLines", FSHAction.Import, FSHResource.BusinessLines),
        new("Clean BusinessLines", FSHAction.Clean, FSHResource.BusinessLines),

        new("View GroupCategories", FSHAction.View, FSHResource.GroupCategories, IsBasic: true),
        new("Search GroupCategories", FSHAction.Search, FSHResource.GroupCategories, IsBasic: true),
        new("Create GroupCategories", FSHAction.Create, FSHResource.GroupCategories),
        new("Update GroupCategories", FSHAction.Update, FSHResource.GroupCategories),
        new("Delete GroupCategories", FSHAction.Delete, FSHResource.GroupCategories),
        new("Export GroupCategories", FSHAction.Export, FSHResource.GroupCategories),
        new("Import GroupCategories", FSHAction.Import, FSHResource.GroupCategories),
        new("Clean GroupCategories", FSHAction.Clean, FSHResource.GroupCategories),

        new("View Categories", FSHAction.View, FSHResource.Categories, IsBasic: true),
        new("Search Categories", FSHAction.Search, FSHResource.Categories, IsBasic: true),
        new("Create Categories", FSHAction.Create, FSHResource.Categories),
        new("Update Categories", FSHAction.Update, FSHResource.Categories),
        new("Delete Categories", FSHAction.Delete, FSHResource.Categories),
        new("Export Categories", FSHAction.Export, FSHResource.Categories),
        new("Import Categories", FSHAction.Import, FSHResource.Categories),
        new("Clean Categories", FSHAction.Clean, FSHResource.Categories),

        new("View SubCategories", FSHAction.View, FSHResource.SubCategories, IsBasic: true),
        new("Search SubCategories", FSHAction.Search, FSHResource.SubCategories, IsBasic: true),
        new("Create SubCategories", FSHAction.Create, FSHResource.SubCategories),
        new("Update SubCategories", FSHAction.Update, FSHResource.SubCategories),
        new("Delete SubCategories", FSHAction.Delete, FSHResource.SubCategories),
        new("Export SubCategories", FSHAction.Export, FSHResource.SubCategories),
        new("Import SubCategories", FSHAction.Import, FSHResource.SubCategories),
        new("Clean SubCategories", FSHAction.Clean, FSHResource.SubCategories),
#endregion

        #region Property
        new("View Assets", FSHAction.View, FSHResource.Assets),
        new("Search Assets", FSHAction.Search, FSHResource.Assets),
        new("Create Assets", FSHAction.Create, FSHResource.Assets),
        new("Update Assets", FSHAction.Update, FSHResource.Assets),
        new("Delete Assets", FSHAction.Delete, FSHResource.Assets),
        new("Export Assets", FSHAction.Export, FSHResource.Assets),
        new("Import Assets", FSHAction.Import, FSHResource.Assets),

        new("View AssetStatuses", FSHAction.View, FSHResource.AssetStatuses),
        new("Search AssetStatuses", FSHAction.Search, FSHResource.AssetStatuses),
        new("Create AssetStatuses", FSHAction.Create, FSHResource.AssetStatuses),
        new("Update AssetStatuses", FSHAction.Update, FSHResource.AssetStatuses),
        new("Delete AssetStatuses", FSHAction.Delete, FSHResource.AssetStatuses),
        new("Export AssetStatuses", FSHAction.Export, FSHResource.AssetStatuses),
        new("Import AssetStatuses", FSHAction.Import, FSHResource.AssetStatuses),

        new("View AssetHistorys", FSHAction.View, FSHResource.AssetHistorys),
        new("Search AssetHistorys", FSHAction.Search, FSHResource.AssetHistorys),
        new("Create AssetHistorys", FSHAction.Create, FSHResource.AssetHistorys),
        new("Update AssetHistorys", FSHAction.Update, FSHResource.AssetHistorys),
        new("Delete AssetHistorys", FSHAction.Delete, FSHResource.AssetHistorys),
        new("Export AssetHistorys", FSHAction.Export, FSHResource.AssetHistorys),
        new("Import AssetHistorys", FSHAction.Import, FSHResource.AssetHistorys),

#endregion

        #region Price
        new("View PriceGroups", FSHAction.View, FSHResource.PriceGroups),
        new("Search PriceGroups", FSHAction.Search, FSHResource.PriceGroups),
        new("Create PriceGroups", FSHAction.Create, FSHResource.PriceGroups),
        new("Update PriceGroups", FSHAction.Update, FSHResource.PriceGroups),
        new("Delete PriceGroups", FSHAction.Delete, FSHResource.PriceGroups),
        new("Export PriceGroups", FSHAction.Export, FSHResource.PriceGroups),
        new("Import PriceGroups", FSHAction.Import, FSHResource.PriceGroups),

        new("View PricePlans", FSHAction.View, FSHResource.PricePlans),
        new("Search PricePlans", FSHAction.Search, FSHResource.PricePlans),
        new("Create PricePlans", FSHAction.Create, FSHResource.PricePlans),
        new("Update PricePlans", FSHAction.Update, FSHResource.PricePlans),
        new("Delete PricePlans", FSHAction.Delete, FSHResource.PricePlans),
        new("Export PricePlans", FSHAction.Export, FSHResource.PricePlans),
        new("Import PricePlans", FSHAction.Import, FSHResource.PricePlans),
        #endregion

        #region Production
        new("View Products", FSHAction.View, FSHResource.Products, IsBasic: true),
        new("Search Products", FSHAction.Search, FSHResource.Products, IsBasic: true),
        new("Create Products", FSHAction.Create, FSHResource.Products),
        new("Update Products", FSHAction.Update, FSHResource.Products),
        new("Delete Products", FSHAction.Delete, FSHResource.Products),
        new("Export Products", FSHAction.Export, FSHResource.Products),
        new("Import Products", FSHAction.Import, FSHResource.Products),
        new("Clean Products", FSHAction.Clean, FSHResource.Products),

        #endregion
    };

    public static IReadOnlyList<FSHPermission> All { get; } = new ReadOnlyCollection<FSHPermission>(_all);
    public static IReadOnlyList<FSHPermission> Root { get; } = new ReadOnlyCollection<FSHPermission>(_all.Where(p => p.IsRoot).ToArray());
    public static IReadOnlyList<FSHPermission> Admin { get; } = new ReadOnlyCollection<FSHPermission>(_all.Where(p => !p.IsRoot).ToArray());
    public static IReadOnlyList<FSHPermission> Basic { get; } = new ReadOnlyCollection<FSHPermission>(_all.Where(p => p.IsBasic).ToArray());
}

public record FSHPermission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    public string Name => NameFor(Action, Resource);
    public static string NameFor(string action, string resource) => $"Permissions.{resource}.{action}";
}
