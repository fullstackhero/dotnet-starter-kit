import {
  Activity,
  Building2,
  LayoutDashboard,
  Receipt,
  ScrollText,
  Settings,
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
  WebhooksPermissions,
} from "@/lib/permissions";

/**
 * A single nav destination. `label` is a `nav` namespace translation key
 * (not display text) — render sites resolve it via `t(item.label)`.
 */
export type NavSpec = {
  to: string;
  label: string;
  icon: LucideIcon;
  /** One or more permissions the user must hold to see this item. */
  perms?: readonly string[];
};

/** A collapsible section that groups related NavSpecs. `caption` is a `nav` key. */
export type NavSection = {
  id: string;
  caption: string;
  icon: LucideIcon;
  items: NavSpec[];
};

// ─── Top-level singletons ────────────────────────────────────────────────────

export const topNavTop: NavSpec[] = [
  { to: "/", label: "overview", icon: LayoutDashboard },
];

export const topNavBottom: NavSpec[] = [
  { to: "/settings", label: "settings", icon: Settings },
];

// ─── Section accordions ──────────────────────────────────────────────────────

export const sections: NavSection[] = [
  {
    id: "multitenancy",
    caption: "tenants",
    icon: Building2,
    items: [
      {
        to: "/tenants",
        label: "tenants",
        icon: Building2,
        perms: [MultitenancyPermissions.Tenants.View],
      },
    ],
  },
  {
    id: "identity",
    caption: "identity",
    icon: UsersRound,
    items: [
      {
        to: "/users",
        label: "users",
        icon: UsersRound,
        perms: [IdentityPermissions.Users.View],
      },
      {
        to: "/roles",
        label: "roles",
        icon: ShieldCheck,
        perms: [IdentityPermissions.Roles.View],
      },
      {
        to: "/impersonation",
        label: "impersonation",
        icon: UserCog,
        perms: [IdentityPermissions.Impersonation.View],
      },
    ],
  },
  {
    id: "operations",
    caption: "operations",
    icon: Activity,
    items: [
      {
        to: "/billing",
        label: "billing",
        icon: Receipt,
        perms: [BillingPermissions.View],
      },
      {
        to: "/webhooks",
        label: "webhooks",
        icon: Webhook,
        perms: [WebhooksPermissions.Subscriptions.View],
      },
      {
        to: "/audits",
        label: "audits",
        icon: ScrollText,
        perms: [AuditingPermissions.AuditTrails.View],
      },
      {
        to: "/health",
        label: "health",
        icon: Activity,
      },
    ],
  },
];

// ─── Helpers ─────────────────────────────────────────────────────────────────

/** Find the section id whose items contain the given path (best prefix match). */
export function findSectionForPath(pathname: string): string | null {
  let bestId: string | null = null;
  let bestLen = 0;
  for (const s of sections) {
    for (const item of s.items) {
      if (
        (item.to === "/" && pathname === "/") ||
        (item.to !== "/" && pathname.startsWith(item.to))
      ) {
        if (item.to.length > bestLen) {
          bestLen = item.to.length;
          bestId = s.id;
        }
      }
    }
  }
  return bestId;
}

/** Returns true when the given NavSpec is the active route. */
export function isNavItemActive(item: NavSpec, pathname: string): boolean {
  if (item.to === "/") return pathname === "/";
  return pathname === item.to || pathname.startsWith(`${item.to}/`);
}

/** Filter nav items (and sections) based on granted permissions. */
export function filterNavSpec(items: NavSpec[], granted: readonly string[]): NavSpec[] {
  return items.filter((item) => {
    if (!item.perms || item.perms.length === 0) return true;
    return item.perms.every((p) => granted.includes(p));
  });
}
