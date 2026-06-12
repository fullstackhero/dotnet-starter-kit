import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { Command } from "cmdk";
import {
  Activity,
  Boxes,
  Folder,
  HeartPulse,
  KeyRound,
  LayoutDashboard,
  LifeBuoy,
  LogOut,
  MessageSquare,
  Monitor,
  Moon,
  Package,
  Palette,
  Plus,
  Receipt,
  ScrollText,
  Search,
  Settings as SettingsIcon,
  Shield,
  ShieldCheck,
  Sparkles,
  Sun,
  Tag,
  Users,
  UserRound,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { useAuth } from "@/auth/use-auth";
import { useTheme } from "@/components/theme/theme-provider";
import { accents } from "@/components/theme/appearance-options";
import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";
import { cn } from "@/lib/cn";

/**
 * Command palette dialog — separated from the provider so cmdk + the full
 * action graph (lucide icons, accent options, navigate logic) are
 * code-split into their own chunk. The provider in command-palette.tsx
 * lazy-imports this module on first ⌘K, keeping the main shell shipping
 * a smaller bundle for cold start.
 */

type ActionItem = {
  id: string;
  label: string;
  hint?: string;
  Icon: React.ComponentType<{ className?: string }>;
  /** Free-form keywords for fuzzy matching. */
  keywords?: string[];
  shortcut?: string;
  perform: () => void;
  /**
   * Permission gates — same semantics as NavSpec in layout/nav-data.ts: the item
   * is hidden unless the user holds `perm` AND at least one of `anyPerm`. Each
   * value mirrors what the destination page's API (or the create action's
   * endpoint) enforces server-side, so the palette never offers a guaranteed 403.
   */
  perm?: string;
  anyPerm?: readonly string[];
};

type ActionGroup = {
  heading: string;
  items: ActionItem[];
};

export function CommandPaletteDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
}) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { setMode, setAccent } = useTheme();
  const permissions = useMemo(() => user?.permissions ?? [], [user]);

  // Build the action set fresh each time the palette opens. The ones
  // that navigate close the palette; the ones that mutate appearance
  // don't, so the user can preview multiple choices.
  const groups = useMemo<ActionGroup[]>(() => {
    const close = () => onOpenChange(false);
    const go = (path: string) => () => {
      navigate(path);
      close();
    };
    // Mirrors isNavItemVisible in layout/nav-data.ts.
    const visible = (item: ActionItem) => {
      if (item.perm && !permissions.includes(item.perm)) return false;
      if (item.anyPerm && !item.anyPerm.some((p) => permissions.includes(p))) return false;
      return true;
    };
    const allGroups: ActionGroup[] = [
      {
        heading: "Navigate",
        items: [
          {
            id: "nav-overview",
            label: "Overview",
            hint: "Tenant telemetry & usage",
            Icon: LayoutDashboard,
            keywords: ["home", "dashboard"],
            perform: go("/"),
          },
          {
            id: "nav-activity",
            label: "Live activity",
            hint: "Real-time event stream",
            Icon: Activity,
            keywords: ["events", "sse", "log"],
            perform: go("/activity"),
          },
          {
            id: "nav-chat",
            label: "Chat",
            hint: "Channels & direct messages",
            Icon: MessageSquare,
            keywords: ["messages", "dm", "channel", "conversation"],
            perform: go("/chat"),
            perm: "Permissions.Chat.Channels.View",
          },
          {
            id: "nav-files",
            label: "Files",
            hint: "My uploaded assets",
            Icon: Folder,
            keywords: ["storage", "uploads", "documents"],
            perform: go("/files"),
            perm: "Permissions.Files.Upload",
          },
          {
            id: "nav-users",
            label: "Users",
            hint: "Identity directory",
            Icon: Users,
            keywords: ["identity", "people", "members", "team"],
            perform: go("/identity/users"),
            perm: "Permissions.Users.Update",
          },
          {
            id: "nav-roles",
            label: "Roles",
            hint: "Permissions & role assignment",
            Icon: ShieldCheck,
            keywords: ["identity", "permissions", "rbac"],
            perform: go("/identity/roles"),
            perm: "Permissions.Roles.Update",
          },
          {
            id: "nav-groups",
            label: "Groups",
            hint: "Org groups & membership",
            Icon: Users,
            keywords: ["identity", "teams", "org"],
            perform: go("/identity/groups"),
            perm: "Permissions.Groups.Update",
          },
          {
            id: "nav-products",
            label: "Products",
            hint: "Catalog inventory",
            Icon: Package,
            keywords: ["catalog", "sku", "inventory", "stock"],
            perform: go("/catalog/products"),
            perm: "Permissions.Catalog.Products.View",
          },
          {
            id: "nav-brands",
            label: "Brands",
            hint: "Catalog brands",
            Icon: Tag,
            keywords: ["catalog"],
            perform: go("/catalog/brands"),
            perm: "Permissions.Catalog.Brands.View",
          },
          {
            id: "nav-categories",
            label: "Categories",
            hint: "Catalog categories",
            Icon: Boxes,
            keywords: ["catalog"],
            perform: go("/catalog/categories"),
            perm: "Permissions.Catalog.Categories.View",
          },
          {
            id: "nav-tickets",
            label: "Tickets",
            hint: "Support requests",
            Icon: LifeBuoy,
            keywords: ["support", "issues", "helpdesk"],
            perform: go("/tickets"),
            perm: "Permissions.Tickets.View",
          },
          {
            id: "nav-invoices",
            label: "Invoices",
            hint: "Billing history",
            Icon: Receipt,
            keywords: ["billing", "payment"],
            perform: go("/invoices"),
            perm: "Permissions.Billing.View",
          },
          {
            id: "nav-health",
            label: "Health",
            hint: "Readiness probe & dependencies",
            Icon: HeartPulse,
            keywords: ["status", "uptime", "system", "ready", "redis", "postgres"],
            perform: go("/system/health"),
          },
          {
            id: "nav-audits",
            label: "Audit trail",
            hint: "Activity, security, entity-change events",
            Icon: ScrollText,
            keywords: ["audit", "log", "compliance", "security", "trace", "correlation"],
            perform: go("/system/audits"),
            perm: "Permissions.AuditTrails.View",
          },
          {
            id: "nav-trash",
            label: "Trash",
            hint: "Soft-deleted records",
            Icon: ScrollText,
            keywords: ["recycle", "deleted", "restore"],
            perform: go("/system/trash"),
            anyPerm: ALL_TRASH_PERMISSIONS,
          },
          {
            id: "nav-sessions",
            label: "Sessions",
            hint: "Active user sessions",
            Icon: Shield,
            keywords: ["devices", "logins"],
            perform: go("/system/sessions"),
            perm: "Permissions.Sessions.ViewAll",
          },
          {
            id: "nav-settings",
            label: "Settings",
            Icon: SettingsIcon,
            keywords: ["preferences", "config"],
            perform: go("/settings"),
          },
        ],
      },
      {
        heading: "Create",
        items: [
          {
            id: "create-user",
            label: "Create user",
            hint: "Register a new account",
            Icon: Plus,
            keywords: ["new", "invite", "register", "identity"],
            perform: go("/identity/users?action=create"),
            perm: "Permissions.Users.Create",
          },
          {
            id: "create-role",
            label: "Create role",
            hint: "Define a new permission set",
            Icon: Plus,
            keywords: ["new", "permissions", "rbac"],
            perform: go("/identity/roles?action=create"),
            perm: "Permissions.Roles.Create",
          },
          {
            id: "create-group",
            label: "Create group",
            hint: "Organize members",
            Icon: Plus,
            keywords: ["new", "team", "org"],
            perform: go("/identity/groups?action=create"),
            perm: "Permissions.Groups.Create",
          },
          {
            id: "create-product",
            label: "Create product",
            hint: "Add to catalog",
            Icon: Plus,
            keywords: ["new", "catalog", "sku"],
            perform: go("/catalog/products?action=create"),
            perm: "Permissions.Catalog.Products.Create",
          },
          {
            id: "create-brand",
            label: "Create brand",
            hint: "Add a catalog brand",
            Icon: Plus,
            keywords: ["new", "catalog"],
            perform: go("/catalog/brands?action=create"),
            perm: "Permissions.Catalog.Brands.Create",
          },
          {
            id: "create-category",
            label: "Create category",
            hint: "Add a catalog category",
            Icon: Plus,
            keywords: ["new", "catalog"],
            perform: go("/catalog/categories?action=create"),
            perm: "Permissions.Catalog.Categories.Create",
          },
          {
            id: "create-ticket",
            label: "Create ticket",
            hint: "File a support request",
            Icon: Plus,
            keywords: ["new", "support", "issue"],
            perform: go("/tickets?action=create"),
            perm: "Permissions.Tickets.Create",
          },
          {
            id: "create-channel",
            label: "Create chat channel",
            hint: "Start a new conversation space",
            Icon: Plus,
            keywords: ["new", "chat", "channel"],
            perform: go("/chat?action=create-channel"),
            perm: "Permissions.Chat.Channels.Create",
          },
          {
            id: "create-file",
            label: "Upload file",
            hint: "Add to your storage",
            Icon: Plus,
            keywords: ["new", "upload", "attach"],
            perform: go("/files?action=upload"),
            perm: "Permissions.Files.Upload",
          },
        ],
      },
      {
        heading: "Account",
        items: [
          {
            id: "acc-profile",
            label: "Profile",
            hint: "Name, email, contact",
            Icon: UserRound,
            perform: go("/settings/profile"),
          },
          {
            id: "acc-security",
            label: "Security",
            hint: "Password, 2FA, sessions",
            Icon: Shield,
            keywords: ["password", "2fa", "sessions"],
            perform: go("/settings/security"),
          },
          {
            id: "acc-keys",
            label: "API keys",
            hint: "Generate & rotate",
            Icon: KeyRound,
            keywords: ["token", "credentials"],
            perform: go("/settings/api-keys"),
          },
          {
            id: "acc-notifications",
            label: "Notifications",
            hint: "Email preferences",
            Icon: Sparkles,
            perform: go("/settings/notifications"),
          },
          {
            id: "acc-appearance",
            label: "Appearance",
            hint: "Theme, accent, font, density",
            Icon: Palette,
            keywords: ["theme", "font", "density", "dark", "light"],
            perform: go("/settings/appearance"),
          },
        ],
      },
      {
        heading: "Theme",
        items: [
          {
            id: "theme-light",
            label: "Switch to light",
            Icon: Sun,
            keywords: ["bright", "day"],
            perform: () => setMode("light"),
          },
          {
            id: "theme-dark",
            label: "Switch to dark",
            Icon: Moon,
            keywords: ["night", "oled"],
            perform: () => setMode("dark"),
          },
          {
            id: "theme-system",
            label: "Follow system theme",
            Icon: Monitor,
            keywords: ["auto"],
            perform: () => setMode("system"),
          },
        ],
      },
      {
        heading: "Accent",
        items: accents.map((a) => ({
          id: `accent-${a.id}`,
          label: `Set accent: ${a.label}`,
          hint: a.description,
          Icon: Palette,
          keywords: ["color", "brand", a.id],
          perform: () => setAccent(a.id),
        })),
      },
      {
        heading: "Session",
        items: [
          {
            id: "sess-logout",
            label: "Sign out",
            hint: "End this session",
            Icon: LogOut,
            keywords: ["logout", "exit", "quit"],
            perform: () => {
              close();
              logout();
            },
          },
        ],
      },
    ];
    // Drop items the user can't access, then drop any group left empty —
    // same shape as visibleSections() in layout/nav-data.ts.
    return allGroups
      .map((g) => ({ ...g, items: g.items.filter(visible) }))
      .filter((g) => g.items.length > 0);
  }, [navigate, onOpenChange, setMode, setAccent, logout, permissions]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={cn(
          "max-w-[640px] p-0 sm:max-w-[640px]",
          "bg-[var(--color-popover)]",
        )}
      >
        <DialogTitle className="sr-only">Command palette</DialogTitle>
        <DialogDescription className="sr-only">
          Search across pages, account actions, theme and accent. Use arrow keys to navigate; Enter to select.
        </DialogDescription>

        <Command
          loop
          className="flex flex-col"
          // cmdk sets [cmdk-...] data attrs we hook into with selectors below.
        >
          {/* Search row — mirrors EntitySearch shape (rounded-xl, soft icon left). */}
          <div className="flex items-center gap-2.5 border-b border-border px-4 py-3">
            <Search className="h-[18px] w-[18px] shrink-0 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" aria-hidden />
            <Command.Input
              placeholder="Type a command or search…"
              aria-label="Search commands"
              className={cn(
                "h-7 flex-1 bg-transparent text-[14px] tracking-tight placeholder:text-[var(--color-muted-foreground)]",
                "focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
              )}
              autoFocus
            />
            <kbd className="rounded border border-border bg-[var(--color-muted)] px-1.5 py-px text-[10px] tracking-tight text-[var(--color-muted-foreground)]">
              Esc
            </kbd>
          </div>

          {/* Results */}
          <Command.List className="max-h-[420px] overflow-y-auto px-2 py-2">
            <Command.Empty className="px-4 py-12 text-center">
              <p className="text-sm font-medium tracking-tight">No matches</p>
              <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                Try a different keyword — page name, entity, theme, accent, or sign-out.
              </p>
            </Command.Empty>

            {groups.map((group) => (
              <Command.Group
                key={group.heading}
                heading={group.heading}
                className={cn(
                  // Heading text styling via cmdk's nested rendering.
                  "[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:pt-3",
                  "[&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-semibold",
                  "[&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-wider",
                  "[&_[cmdk-group-heading]]:text-[var(--color-muted-foreground)]",
                )}
              >
                {group.items.map((item) => (
                  <CommandRow key={item.id} item={item} />
                ))}
              </Command.Group>
            ))}
          </Command.List>

          {/* Footer */}
          <div className="flex items-center justify-between border-t border-border px-4 py-2.5">
            <div className="flex items-center gap-3 text-[11px] text-[var(--color-muted-foreground)]">
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↑</kbd>
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↓</kbd>
                navigate
              </span>
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↵</kbd>
                select
              </span>
            </div>
            <span className="text-[11px] text-[var(--color-muted-foreground)]">
              v0.1
            </span>
          </div>
        </Command>
      </DialogContent>
    </Dialog>
  );
}

function CommandRow({ item }: { item: ActionItem }) {
  const { Icon, label, hint, keywords, perform } = item;
  return (
    <Command.Item
      value={[label, hint, ...(keywords ?? [])].filter(Boolean).join(" ")}
      onSelect={perform}
      className={cn(
        "group/cmd flex cursor-default select-none items-center gap-3 rounded-md px-2.5 py-2 text-sm",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "outline-none focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
        "hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]",
        "data-[selected=true]:bg-[var(--color-primary-soft)] data-[selected=true]:text-[var(--color-foreground)]",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "grid h-7 w-7 shrink-0 place-items-center rounded-md",
          "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
          "transition-colors group-data-[selected=true]/cmd:bg-[var(--color-primary-soft)] group-data-[selected=true]/cmd:text-[var(--color-primary)]",
        )}
      >
        <Icon className="h-3.5 w-3.5" />
      </span>
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="truncate font-medium tracking-tight">{label}</span>
        {hint && (
          <span className="truncate text-[11px] text-[var(--color-muted-foreground)]">
            {hint}
          </span>
        )}
      </span>
    </Command.Item>
  );
}
