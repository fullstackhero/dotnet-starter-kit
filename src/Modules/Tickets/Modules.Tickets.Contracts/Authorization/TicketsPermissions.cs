using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Tickets.Contracts.Authorization;

public static class TicketsPermissions
{
    public static class Tickets
    {
        public const string Resource = "Tickets";
        public const string View     = $"Permissions.{Resource}.View";
        public const string Create   = $"Permissions.{Resource}.Create";
        public const string Update   = $"Permissions.{Resource}.Update";
        public const string Delete   = $"Permissions.{Resource}.Delete";
        public const string Restore  = $"Permissions.{Resource}.Restore";
        public const string Assign   = $"Permissions.{Resource}.Assign";
        public const string Resolve  = $"Permissions.{Resource}.Resolve";
        public const string Reopen   = $"Permissions.{Resource}.Reopen";
        public const string Close    = $"Permissions.{Resource}.Close";
        public const string Comment  = $"Permissions.{Resource}.Comment";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Tickets",    ActionConstants.View,   Tickets.Resource, IsBasic: true),
        new("Create Tickets",  ActionConstants.Create, Tickets.Resource),
        new("Update Tickets",  ActionConstants.Update, Tickets.Resource),
        new("Delete Tickets",  ActionConstants.Delete, Tickets.Resource),
        new("Restore Tickets", "Restore", Tickets.Resource),
        new("Assign Tickets",  "Assign",  Tickets.Resource),
        new("Resolve Tickets", "Resolve", Tickets.Resource),
        new("Reopen Tickets",  "Reopen",  Tickets.Resource),
        new("Close Tickets",   "Close",   Tickets.Resource),
        new("Comment on Tickets", "Comment", Tickets.Resource),
    ];
}
