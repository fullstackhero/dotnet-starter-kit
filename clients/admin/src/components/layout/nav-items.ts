import {
  Activity,
  Building2,
  LayoutDashboard,
  Receipt,
  ScrollText,
  ShieldCheck,
  UserCog,
  UsersRound,
  Webhook,
  type LucideIcon,
} from "lucide-react";
import {
  AuditingPermissions,
  BillingPermissions,
  IdentityPermissions,
  MultitenancyPermissions,
} from "@/lib/permissions";

export type NavItem = {
  to: string;
  label: string;
  icon: LucideIcon;
  /** Sub-path prefix that should also light this nav item. */
  matchPrefix?: string;
  /**
   * Permissions the user must hold to see this item in the sidebar. Mirrors
   * the route's RouteGuard exactly — if the user can't reach the page they
   * shouldn't see the link. Omit (or pass []) for surfaces every signed-in
   * user can hit (Overview, Health).
   */
  perms?: readonly string[];
};

/**
 * Primary navigation. Shared between the desktop sidebar and the mobile
 * drawer so the magazine-table-of-contents numbering stays consistent.
 *
 * Per-entry `perms` are kept in lockstep with routes.tsx — see useVisibleNavItems
 * for the filtering. If a route's gating changes, update both places.
 */
export const NAV_ITEMS: NavItem[] = [
  { to: "/", label: "Overview", icon: LayoutDashboard },
  {
    to: "/tenants",
    label: "Tenants",
    icon: Building2,
    matchPrefix: "/tenants",
    perms: [MultitenancyPermissions.Tenants.View],
  },
  {
    to: "/users",
    label: "Users",
    icon: UsersRound,
    matchPrefix: "/users",
    perms: [IdentityPermissions.Users.View],
  },
  {
    to: "/roles",
    label: "Roles",
    icon: ShieldCheck,
    matchPrefix: "/roles",
    perms: [IdentityPermissions.Roles.View],
  },
  {
    to: "/billing",
    label: "Billing",
    icon: Receipt,
    matchPrefix: "/billing",
    perms: [BillingPermissions.View],
  },
  {
    to: "/impersonation",
    label: "Impersonation",
    icon: UserCog,
    matchPrefix: "/impersonation",
    perms: [IdentityPermissions.Impersonation.View],
  },
  {
    to: "/audits",
    label: "Audits",
    icon: ScrollText,
    matchPrefix: "/audits",
    perms: [AuditingPermissions.AuditTrails.View],
  },
  {
    to: "/webhooks",
    label: "Webhooks",
    icon: Webhook,
    matchPrefix: "/webhooks",
  },
  { to: "/health", label: "Health", icon: Activity, matchPrefix: "/health" },
];

export function isNavItemActive(item: NavItem, pathname: string): boolean {
  if (item.matchPrefix) {
    return pathname === item.matchPrefix || pathname.startsWith(`${item.matchPrefix}/`);
  }
  return pathname === item.to;
}

export function filterNavItems(items: NavItem[], grantedPermissions: readonly string[]): NavItem[] {
  return items.filter((item) => {
    if (!item.perms || item.perms.length === 0) return true;
    return item.perms.every((p) => grantedPermissions.includes(p));
  });
}
