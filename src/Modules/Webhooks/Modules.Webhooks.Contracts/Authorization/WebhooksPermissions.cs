using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Webhooks.Contracts.Authorization;

public static class WebhooksPermissions
{
    public static class Subscriptions
    {
        public const string Resource = "Webhooks";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Delete = $"Permissions.{Resource}.Delete";
        public const string Test   = $"Permissions.{Resource}.Test";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Webhooks",   ActionConstants.View,   Subscriptions.Resource, IsBasic: true),
        new("Create Webhooks", ActionConstants.Create, Subscriptions.Resource),
        new("Delete Webhooks", ActionConstants.Delete, Subscriptions.Resource),
        new("Test Webhooks",   "Test",                 Subscriptions.Resource),
    ];
}
