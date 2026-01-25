namespace FSH.Framework.Shared.Identity;

public static class IdentityPermissionConstants
{
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Update = "Permissions.Users.Update";
        public const string Delete = "Permissions.Users.Delete";
        public const string ManageRoles = "Permissions.Users.ManageRoles";
    }

    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        public const string Create = "Permissions.Roles.Create";
        public const string Update = "Permissions.Roles.Update";
        public const string Delete = "Permissions.Roles.Delete";
    }

    public static class Sessions
    {
        public const string View = "Permissions.Sessions.View";
        public const string Revoke = "Permissions.Sessions.Revoke";
        public const string ViewAll = "Permissions.Sessions.ViewAll";
        public const string RevokeAll = "Permissions.Sessions.RevokeAll";
    }

    public static class Groups
    {
        public const string View = "Permissions.Groups.View";
        public const string Create = "Permissions.Groups.Create";
        public const string Update = "Permissions.Groups.Update";
        public const string Delete = "Permissions.Groups.Delete";
        public const string ManageMembers = "Permissions.Groups.ManageMembers";
    }
}
