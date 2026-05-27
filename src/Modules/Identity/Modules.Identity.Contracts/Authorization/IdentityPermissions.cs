using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Identity.Contracts.Authorization;

/// <summary>
/// Identity module permissions. Single source of truth — string literals (used by
/// <c>.RequirePermission(...)</c>) and the <see cref="All"/> registry list are derived
/// from the same Resource/Action constants below, so they cannot drift.
/// </summary>
public static class IdentityPermissions
{
    public static class Users
    {
        public const string Resource = nameof(Users);
        public const string View          = $"Permissions.{Resource}.View";
        public const string Search        = $"Permissions.{Resource}.Search";
        public const string Create        = $"Permissions.{Resource}.Create";
        public const string Update        = $"Permissions.{Resource}.Update";
        public const string Delete        = $"Permissions.{Resource}.Delete";
        public const string Export        = $"Permissions.{Resource}.Export";
        public const string ManageRoles   = $"Permissions.{Resource}.ManageRoles";
        public const string Impersonate   = $"Permissions.{Resource}.Impersonate";
    }

    public static class UserRoles
    {
        public const string Resource = nameof(UserRoles);
        public const string View   = $"Permissions.{Resource}.View";
        public const string Update = $"Permissions.{Resource}.Update";
    }

    public static class Roles
    {
        public const string Resource = nameof(Roles);
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class RoleClaims
    {
        public const string Resource = nameof(RoleClaims);
        public const string View   = $"Permissions.{Resource}.View";
        public const string Update = $"Permissions.{Resource}.Update";
    }

    public static class Sessions
    {
        public const string Resource = nameof(Sessions);
        public const string View      = $"Permissions.{Resource}.View";
        public const string Revoke    = $"Permissions.{Resource}.Revoke";
        public const string ViewAll   = $"Permissions.{Resource}.ViewAll";
        public const string RevokeAll = $"Permissions.{Resource}.RevokeAll";
    }

    public static class Groups
    {
        public const string Resource = nameof(Groups);
        public const string View          = $"Permissions.{Resource}.View";
        public const string Create        = $"Permissions.{Resource}.Create";
        public const string Update        = $"Permissions.{Resource}.Update";
        public const string Delete        = $"Permissions.{Resource}.Delete";
        public const string ManageMembers = $"Permissions.{Resource}.ManageMembers";
    }

    public static class Impersonation
    {
        public const string Resource = nameof(Impersonation);
        /// <summary>List impersonation grants (read-only access to the grant history).</summary>
        public const string View   = $"Permissions.{Resource}.View";
        /// <summary>Revoke an active impersonation grant before its natural expiry.</summary>
        public const string Revoke = $"Permissions.{Resource}.Revoke";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Users",          ActionConstants.View,   Users.Resource, IsBasic: true),
        new("Search Users",        ActionConstants.Search, Users.Resource),
        new("Create Users",        ActionConstants.Create, Users.Resource),
        new("Update Users",        ActionConstants.Update, Users.Resource),
        new("Delete Users",        ActionConstants.Delete, Users.Resource),
        new("Export Users",        ActionConstants.Export, Users.Resource),
        new("Manage User Roles",   "ManageRoles",          Users.Resource),
        new("Impersonate User",    "Impersonate",          Users.Resource),

        new("View User Roles",     ActionConstants.View,   UserRoles.Resource, IsBasic: true),
        new("Update User Roles",   ActionConstants.Update, UserRoles.Resource),

        new("View Roles",          ActionConstants.View,   Roles.Resource, IsBasic: true),
        new("Create Roles",        ActionConstants.Create, Roles.Resource),
        new("Update Roles",        ActionConstants.Update, Roles.Resource),
        new("Delete Roles",        ActionConstants.Delete, Roles.Resource),

        new("View Role Claims",    ActionConstants.View,   RoleClaims.Resource, IsBasic: true),
        new("Update Role Claims",  ActionConstants.Update, RoleClaims.Resource),

        new("View My Sessions",    ActionConstants.View,    Sessions.Resource, IsBasic: true),
        new("Revoke My Sessions",  "Revoke",                Sessions.Resource, IsBasic: true),
        new("View All Sessions",   "ViewAll",               Sessions.Resource),
        new("Revoke Any Session",  "RevokeAll",             Sessions.Resource),

        new("View Groups",         ActionConstants.View,   Groups.Resource, IsBasic: true),
        new("Create Groups",       ActionConstants.Create, Groups.Resource),
        new("Update Groups",       ActionConstants.Update, Groups.Resource),
        new("Delete Groups",       ActionConstants.Delete, Groups.Resource),
        new("Manage Group Members","ManageMembers",        Groups.Resource),

        new("View Impersonation Grants",   ActionConstants.View, Impersonation.Resource),
        new("Revoke Impersonation Grants", "Revoke",             Impersonation.Resource),
    ];
}
