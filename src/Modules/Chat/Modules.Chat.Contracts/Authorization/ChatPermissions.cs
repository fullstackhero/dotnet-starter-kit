using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Chat.Contracts.Authorization;

/// <summary>
/// Permission constants for the Chat module. Permission names follow the
/// <c>Permissions.{Resource}.{Action}</c> shape per the framework convention.
/// </summary>
public static class ChatPermissions
{
    public static class Channels
    {
        public const string Resource  = "Chat.Channels";
        public const string View      = $"Permissions.{Resource}.View";
        public const string Create    = $"Permissions.{Resource}.Create";
        public const string ManageAll = $"Permissions.{Resource}.ManageAll";
    }

    public static class Messages
    {
        public const string Resource  = "Chat.Messages";
        public const string Send      = $"Permissions.{Resource}.Send";
        public const string EditOwn   = $"Permissions.{Resource}.EditOwn";
        public const string DeleteOwn = $"Permissions.{Resource}.DeleteOwn";
        public const string DeleteAny = $"Permissions.{Resource}.DeleteAny";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Chat Channels",   ActionConstants.View,   Channels.Resource, IsBasic: true),
        new("Create Chat Channels", ActionConstants.Create, Channels.Resource, IsBasic: true),
        new("Manage All Channels",  "ManageAll",            Channels.Resource),
        new("Send Messages",        "Send",                 Messages.Resource, IsBasic: true),
        new("Edit Own Messages",    "EditOwn",              Messages.Resource, IsBasic: true),
        new("Delete Own Messages",  "DeleteOwn",            Messages.Resource, IsBasic: true),
        new("Delete Any Message",   "DeleteAny",            Messages.Resource),
    ];
}
