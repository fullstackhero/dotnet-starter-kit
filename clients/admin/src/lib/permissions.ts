/**
 * Permission strings + catalog mirrored from the server's *Permissions.cs
 * registries. Kept here so:
 *   1. Route guards stay typo-proof (`IdentityPermissions.Users.View`).
 *   2. The Role editor can render every assignable permission grouped
 *      by category without an extra round-trip.
 *
 * If the server registry adds permissions, mirror them here — there is no
 * runtime fetch (no /permissions catalog endpoint exists).
 * Convention follows the server: `Permissions.{Resource}.{Action}`.
 */

export const IdentityPermissions = Object.freeze({
  Users: {
    View: "Permissions.Users.View",
    Search: "Permissions.Users.Search",
    Create: "Permissions.Users.Create",
    Update: "Permissions.Users.Update",
    Delete: "Permissions.Users.Delete",
    Export: "Permissions.Users.Export",
    ManageRoles: "Permissions.Users.ManageRoles",
    Impersonate: "Permissions.Users.Impersonate",
  },
  UserRoles: {
    View: "Permissions.UserRoles.View",
    Update: "Permissions.UserRoles.Update",
  },
  Roles: {
    View: "Permissions.Roles.View",
    Create: "Permissions.Roles.Create",
    Update: "Permissions.Roles.Update",
    Delete: "Permissions.Roles.Delete",
  },
  RoleClaims: {
    View: "Permissions.RoleClaims.View",
    Update: "Permissions.RoleClaims.Update",
  },
  Sessions: {
    View: "Permissions.Sessions.View",
    Revoke: "Permissions.Sessions.Revoke",
    ViewAll: "Permissions.Sessions.ViewAll",
    RevokeAll: "Permissions.Sessions.RevokeAll",
  },
  Impersonation: {
    View: "Permissions.Impersonation.View",
    Revoke: "Permissions.Impersonation.Revoke",
  },
} as const);

export const MultitenancyPermissions = Object.freeze({
  Tenants: {
    View: "Permissions.Tenants.View",
    Create: "Permissions.Tenants.Create",
    Update: "Permissions.Tenants.Update",
    UpgradeSubscription: "Permissions.Tenants.UpgradeSubscription",
  },
} as const);

export const BillingPermissions = Object.freeze({
  View: "Permissions.Billing.View",
  Manage: "Permissions.Billing.Manage",
} as const);

export const AuditingPermissions = Object.freeze({
  AuditTrails: {
    View: "Permissions.AuditTrails.View",
    ViewCrossTenant: "Permissions.AuditTrails.ViewCrossTenant",
  },
} as const);

export const WebhooksPermissions = Object.freeze({
  Subscriptions: {
    View: "Permissions.Webhooks.View",
    Create: "Permissions.Webhooks.Create",
    Delete: "Permissions.Webhooks.Delete",
    Test: "Permissions.Webhooks.Test",
  },
} as const);

// ─── Catalog (drives the Role editor) ───────────────────────────────────

export type PermissionEntry = {
  name: string;
  description: string;
  /** Only assignable on root-tenant (cross-tenant) roles. */
  root?: boolean;
  /** Granted by default to authenticated users via the basic role. */
  basic?: boolean;
};

export type PermissionGroup = {
  /** UI-facing category name. */
  category: string;
  /** Section blurb shown under the heading. */
  blurb: string;
  entries: PermissionEntry[];
};

export const PERMISSION_CATALOG: readonly PermissionGroup[] = [
  {
    category: "Tenants",
    blurb: "Provision and operate tenants. Reserved for the root-tenant operator.",
    entries: [
      { name: MultitenancyPermissions.Tenants.View, description: "View tenants", root: true },
      { name: MultitenancyPermissions.Tenants.Create, description: "Create tenants", root: true },
      { name: MultitenancyPermissions.Tenants.Update, description: "Update tenants", root: true },
      { name: MultitenancyPermissions.Tenants.UpgradeSubscription, description: "Upgrade tenant subscription", root: true },
    ],
  },
  {
    category: "Users",
    blurb: "Manage tenant user accounts and their assigned roles.",
    entries: [
      { name: IdentityPermissions.Users.View, description: "View users", basic: true },
      { name: IdentityPermissions.Users.Search, description: "Search users" },
      { name: IdentityPermissions.Users.Create, description: "Create users" },
      { name: IdentityPermissions.Users.Update, description: "Update users" },
      { name: IdentityPermissions.Users.Delete, description: "Delete users" },
      { name: IdentityPermissions.Users.Export, description: "Export users" },
      { name: IdentityPermissions.Users.ManageRoles, description: "Assign roles to users" },
      { name: IdentityPermissions.Users.Impersonate, description: "Impersonate another user" },
    ],
  },
  {
    category: "Roles",
    blurb: "Manage role definitions and their permission grants.",
    entries: [
      { name: IdentityPermissions.Roles.View, description: "View roles", basic: true },
      { name: IdentityPermissions.Roles.Create, description: "Create roles" },
      { name: IdentityPermissions.Roles.Update, description: "Update roles" },
      { name: IdentityPermissions.Roles.Delete, description: "Delete roles" },
      { name: IdentityPermissions.RoleClaims.View, description: "View role claims", basic: true },
      { name: IdentityPermissions.RoleClaims.Update, description: "Update role claims" },
      { name: IdentityPermissions.UserRoles.View, description: "View user-role assignments", basic: true },
      { name: IdentityPermissions.UserRoles.Update, description: "Update user-role assignments" },
    ],
  },
  {
    category: "Sessions",
    blurb: "View and revoke active sessions.",
    entries: [
      { name: IdentityPermissions.Sessions.View, description: "View my sessions", basic: true },
      { name: IdentityPermissions.Sessions.Revoke, description: "Revoke my sessions", basic: true },
      { name: IdentityPermissions.Sessions.ViewAll, description: "View all tenant sessions" },
      { name: IdentityPermissions.Sessions.RevokeAll, description: "Revoke any session" },
    ],
  },
  {
    category: "Billing",
    blurb: "Inspect and manage tenant subscriptions and invoices.",
    entries: [
      { name: BillingPermissions.View, description: "View billing", basic: true },
      { name: BillingPermissions.Manage, description: "Manage billing — plans, subscriptions, invoices" },
    ],
  },
  {
    category: "Audit trails",
    blurb: "Inspect security and entity-change audit events.",
    entries: [
      { name: AuditingPermissions.AuditTrails.View, description: "View audit trails", basic: true },
      {
        name: AuditingPermissions.AuditTrails.ViewCrossTenant,
        description: "View audit trails across tenants",
        root: true,
      },
    ],
  },
  {
    category: "Impersonation",
    blurb: "Inspect and revoke active impersonation sessions. Revocation invalidates the issued token immediately.",
    entries: [
      { name: IdentityPermissions.Impersonation.View, description: "View impersonation grants" },
      { name: IdentityPermissions.Impersonation.Revoke, description: "Revoke active impersonation grants" },
    ],
  },
  {
    category: "Webhooks",
    blurb: "Manage outbound webhook subscriptions and inspect their deliveries.",
    entries: [
      { name: WebhooksPermissions.Subscriptions.View, description: "View webhook subscriptions & deliveries", basic: true },
      { name: WebhooksPermissions.Subscriptions.Create, description: "Create webhook subscriptions" },
      { name: WebhooksPermissions.Subscriptions.Delete, description: "Delete webhook subscriptions" },
      { name: WebhooksPermissions.Subscriptions.Test, description: "Send test webhook deliveries" },
    ],
  },
];

export const ALL_PERMISSION_NAMES: readonly string[] = PERMISSION_CATALOG.flatMap((g) =>
  g.entries.map((e) => e.name),
);
