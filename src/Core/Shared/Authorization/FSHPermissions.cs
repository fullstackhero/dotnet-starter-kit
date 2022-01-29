using System.ComponentModel;

namespace FSH.WebApi.Shared.Authorization;

public class FSHPermissions
{
    [DisplayName("Dashboard")]
    [Description("Dashboard Permissions")]
    public static class Dashboard
    {
        public const string View = "Permissions.Dashboard.View";
    }

    [DisplayName("AuditLogs")]
    [Description("AuditLogs Permissions")]
    public static class AuditLogs
    {
        public const string View = "Permissions.AuditLogs.View";
    }

    [DisplayName("Hangfire")]
    [Description("Hangfire Permissions")]
    public static class Hangfire
    {
        public const string View = "Permissions.Hangfire.View";
    }

    [DisplayName("Identity")]
    [Description("Identity Permissions")]
    public static class Identity
    {
        public const string Create = "Permissions.Identity.Create";
    }

    [DisplayName("Users")]
    [Description("Users Permissions")]
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Update = "Permissions.Users.Update";
        public const string Delete = "Permissions.Users.Delete";
        public const string Export = "Permissions.Users.Export";
        public const string Search = "Permissions.Users.Search";
    }

    [DisplayName("Roles")]
    [Description("Roles Permissions")]
    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        public const string Create = "Permissions.Roles.Create";
        public const string Update = "Permissions.Roles.Update";
        public const string Delete = "Permissions.Roles.Delete";
    }

    [DisplayName("Role Claims")]
    [Description("Role Claims Permissions")]
    public static class RoleClaims
    {
        public const string View = "Permissions.RoleClaims.View";
        public const string Create = "Permissions.RoleClaims.Create";
        public const string Update = "Permissions.RoleClaims.Update";
        public const string Delete = "Permissions.RoleClaims.Delete";
        public const string Search = "Permissions.RoleClaims.Search";
    }

    [DisplayName("Products")]
    [Description("Products Permissions")]
    public static class Products
    {
        public const string View = "Permissions.Products.View";
        public const string Search = "Permissions.Products.Search";
        public const string Create = "Permissions.Products.Create";
        public const string Update = "Permissions.Products.Update";
        public const string Delete = "Permissions.Products.Delete";
    }

    [DisplayName("Brands")]
    [Description("Brands Permissions")]
    public static class Brands
    {
        public const string View = "Permissions.Brands.View";
        public const string Search = "Permissions.Brands.Search";
        public const string Create = "Permissions.Brands.Create";
        public const string Update = "Permissions.Brands.Update";
        public const string Delete = "Permissions.Brands.Delete";
        public const string Generate = "Permissions.Brands.Generate";
        public const string Clean = "Permissions.Brands.Clean";
    }
}