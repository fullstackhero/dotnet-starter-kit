using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Auditing.Contracts.Authorization;

public static class AuditingPermissions
{
    public static class AuditTrails
    {
        public const string Resource = nameof(AuditTrails);
        public const string View = $"Permissions.{Resource}.View";
        /// <summary>
        /// Allows querying audits across tenants by passing a TenantId filter.
        /// Without this permission, callers can only see their own tenant's audits.
        /// </summary>
        public const string ViewCrossTenant = $"Permissions.{Resource}.ViewCrossTenant";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Audit Trails", ActionConstants.View, AuditTrails.Resource, IsBasic: true),
        new("View Audit Trails Across Tenants", "ViewCrossTenant", AuditTrails.Resource, IsRoot: true),
    ];
}
