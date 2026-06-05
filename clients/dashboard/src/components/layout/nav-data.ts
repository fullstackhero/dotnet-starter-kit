import {
  Activity,
  CreditCard,
  FolderOpen,
  FolderTree,
  HeartPulse,
  LayoutDashboard,
  MessageCircle,
  Package,
  Receipt,
  ScrollText,
  Settings,
  ShieldCheck,
  Tags,
  Ticket,
  Trash2,
  Users,
  UsersRound,
  Wifi,
} from "lucide-react";

export type NavSpec = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /**
   * Permission required to see this item. Items without a `perm` are visible to
   * every authenticated tenant user; gated items are hidden when the current
   * user (or impersonated user) lacks the permission, so they never land on a
   * page the API will reject with 403.
   */
  perm?: string;
};

export type NavSection = {
  id: string;
  caption: string;
  /** Section-level icon used as a fallback when the sidebar is
   *  collapsed and the section is rendered as a stack of item icons. */
  icon: React.ComponentType<{ className?: string }>;
  items: NavSpec[];
};

// Top-level items live OUTSIDE any section. Overview opens the app;
// Settings is account-scoped and lives at the very bottom.
export const topNavTop: NavSpec[] = [
  { to: "/", label: "Overview", icon: LayoutDashboard },
  { to: "/chat", label: "Chat", icon: MessageCircle },
  { to: "/files", label: "My Files", icon: FolderOpen },
];

export const topNavBottom: NavSpec[] = [
  { to: "/settings", label: "Settings", icon: Settings },
];

// Section accordion. Single-select — only one section open at a time.
export const sections: NavSection[] = [
  {
    id: "operations",
    caption: "Operations",
    icon: Activity,
    items: [
      { to: "/activity", label: "Live activity", icon: Activity },
      { to: "/subscription", label: "Subscription", icon: CreditCard },
      { to: "/invoices", label: "Invoices", icon: Receipt },
    ],
  },
  {
    id: "catalog",
    caption: "Catalog",
    icon: Package,
    items: [
      { to: "/catalog/products", label: "Products", icon: Package },
      { to: "/catalog/brands", label: "Brands", icon: Tags },
      { to: "/catalog/categories", label: "Categories", icon: FolderTree },
    ],
  },
  {
    id: "helpdesk",
    caption: "Helpdesk",
    icon: Ticket,
    items: [
      { to: "/tickets", label: "Tickets", icon: Ticket },
    ],
  },
  {
    id: "identity",
    caption: "Identity",
    icon: Users,
    items: [
      // Gate the identity-management pages on a manage permission (not View): View Users/Roles/Groups
      // are IsBasic so every member holds them (the chat/user picker relies on Users.View), but only
      // managers should see these admin pages. Basic lacks the *.Update perms, so the items hide for them.
      { to: "/identity/users", label: "Users", icon: Users, perm: "Permissions.Users.Update" },
      { to: "/identity/roles", label: "Roles", icon: ShieldCheck, perm: "Permissions.Roles.Update" },
      { to: "/identity/groups", label: "Groups", icon: UsersRound, perm: "Permissions.Groups.Update" },
    ],
  },
  {
    id: "system",
    caption: "System",
    icon: HeartPulse,
    items: [
      { to: "/system/health", label: "Health", icon: HeartPulse },
      { to: "/system/audits", label: "Audit trail", icon: ScrollText, perm: "Permissions.AuditTrails.View" },
      { to: "/system/sessions", label: "Sessions", icon: Wifi, perm: "Permissions.Sessions.ViewAll" },
      { to: "/system/trash", label: "Trash", icon: Trash2 },
    ],
  },
];

/** True when the item is ungated, or the user holds its required permission. */
function isNavItemVisible(item: NavSpec, permissions: readonly string[]): boolean {
  return !item.perm || permissions.includes(item.perm);
}

/** Drop items the user can't access, then drop any section left empty. */
export function visibleSections(permissions: readonly string[]): NavSection[] {
  return sections
    .map((s) => ({ ...s, items: s.items.filter((i) => isNavItemVisible(i, permissions)) }))
    .filter((s) => s.items.length > 0);
}

/** Filter a flat nav list (top/bottom) by permission. */
export function visibleItems(items: NavSpec[], permissions: readonly string[]): NavSpec[] {
  return items.filter((i) => isNavItemVisible(i, permissions));
}

/** Find the section whose items contain the given path (best prefix match). */
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
