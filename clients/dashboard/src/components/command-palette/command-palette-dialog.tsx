import { useMemo } from "react";
import { useTranslation } from "react-i18next";
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
  const { t } = useTranslation("commandPalette");
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
        heading: t("group.navigate"),
        items: [
          {
            id: "nav-overview",
            label: t("nav.overview.label"),
            hint: t("nav.overview.hint"),
            Icon: LayoutDashboard,
            keywords: ["home", "dashboard"],
            perform: go("/"),
          },
          {
            id: "nav-activity",
            label: t("nav.activity.label"),
            hint: t("nav.activity.hint"),
            Icon: Activity,
            keywords: ["events", "sse", "log"],
            perform: go("/activity"),
          },
          {
            id: "nav-chat",
            label: t("nav.chat.label"),
            hint: t("nav.chat.hint"),
            Icon: MessageSquare,
            keywords: ["messages", "dm", "channel", "conversation"],
            perform: go("/chat"),
            perm: "Permissions.Chat.Channels.View",
          },
          {
            id: "nav-files",
            label: t("nav.files.label"),
            hint: t("nav.files.hint"),
            Icon: Folder,
            keywords: ["storage", "uploads", "documents"],
            perform: go("/files"),
            perm: "Permissions.Files.Upload",
          },
          {
            id: "nav-users",
            label: t("nav.users.label"),
            hint: t("nav.users.hint"),
            Icon: Users,
            keywords: ["identity", "people", "members", "team"],
            perform: go("/identity/users"),
            perm: "Permissions.Users.Update",
          },
          {
            id: "nav-roles",
            label: t("nav.roles.label"),
            hint: t("nav.roles.hint"),
            Icon: ShieldCheck,
            keywords: ["identity", "permissions", "rbac"],
            perform: go("/identity/roles"),
            perm: "Permissions.Roles.Update",
          },
          {
            id: "nav-groups",
            label: t("nav.groups.label"),
            hint: t("nav.groups.hint"),
            Icon: Users,
            keywords: ["identity", "teams", "org"],
            perform: go("/identity/groups"),
            perm: "Permissions.Groups.Update",
          },
          {
            id: "nav-products",
            label: t("nav.products.label"),
            hint: t("nav.products.hint"),
            Icon: Package,
            keywords: ["catalog", "sku", "inventory", "stock"],
            perform: go("/catalog/products"),
            perm: "Permissions.Catalog.Products.View",
          },
          {
            id: "nav-brands",
            label: t("nav.brands.label"),
            hint: t("nav.brands.hint"),
            Icon: Tag,
            keywords: ["catalog"],
            perform: go("/catalog/brands"),
            perm: "Permissions.Catalog.Brands.View",
          },
          {
            id: "nav-categories",
            label: t("nav.categories.label"),
            hint: t("nav.categories.hint"),
            Icon: Boxes,
            keywords: ["catalog"],
            perform: go("/catalog/categories"),
            perm: "Permissions.Catalog.Categories.View",
          },
          {
            id: "nav-tickets",
            label: t("nav.tickets.label"),
            hint: t("nav.tickets.hint"),
            Icon: LifeBuoy,
            keywords: ["support", "issues", "helpdesk"],
            perform: go("/tickets"),
            perm: "Permissions.Tickets.View",
          },
          {
            id: "nav-invoices",
            label: t("nav.invoices.label"),
            hint: t("nav.invoices.hint"),
            Icon: Receipt,
            keywords: ["billing", "payment"],
            perform: go("/invoices"),
            perm: "Permissions.Billing.View",
          },
          {
            id: "nav-health",
            label: t("nav.health.label"),
            hint: t("nav.health.hint"),
            Icon: HeartPulse,
            keywords: ["status", "uptime", "system", "ready", "redis", "postgres"],
            perform: go("/system/health"),
          },
          {
            id: "nav-audits",
            label: t("nav.audits.label"),
            hint: t("nav.audits.hint"),
            Icon: ScrollText,
            keywords: ["audit", "log", "compliance", "security", "trace", "correlation"],
            perform: go("/system/audits"),
            perm: "Permissions.AuditTrails.View",
          },
          {
            id: "nav-trash",
            label: t("nav.trash.label"),
            hint: t("nav.trash.hint"),
            Icon: ScrollText,
            keywords: ["recycle", "deleted", "restore"],
            perform: go("/system/trash"),
            anyPerm: ALL_TRASH_PERMISSIONS,
          },
          {
            id: "nav-sessions",
            label: t("nav.sessions.label"),
            hint: t("nav.sessions.hint"),
            Icon: Shield,
            keywords: ["devices", "logins"],
            perform: go("/system/sessions"),
            perm: "Permissions.Sessions.ViewAll",
          },
          {
            id: "nav-settings",
            label: t("nav.settings.label"),
            Icon: SettingsIcon,
            keywords: ["preferences", "config"],
            perform: go("/settings"),
          },
        ],
      },
      {
        heading: t("group.create"),
        items: [
          {
            id: "create-user",
            label: t("create.user.label"),
            hint: t("create.user.hint"),
            Icon: Plus,
            keywords: ["new", "invite", "register", "identity"],
            perform: go("/identity/users?action=create"),
            perm: "Permissions.Users.Create",
          },
          {
            id: "create-role",
            label: t("create.role.label"),
            hint: t("create.role.hint"),
            Icon: Plus,
            keywords: ["new", "permissions", "rbac"],
            perform: go("/identity/roles?action=create"),
            perm: "Permissions.Roles.Create",
          },
          {
            id: "create-group",
            label: t("create.group.label"),
            hint: t("create.group.hint"),
            Icon: Plus,
            keywords: ["new", "team", "org"],
            perform: go("/identity/groups?action=create"),
            perm: "Permissions.Groups.Create",
          },
          {
            id: "create-product",
            label: t("create.product.label"),
            hint: t("create.product.hint"),
            Icon: Plus,
            keywords: ["new", "catalog", "sku"],
            perform: go("/catalog/products?action=create"),
            perm: "Permissions.Catalog.Products.Create",
          },
          {
            id: "create-brand",
            label: t("create.brand.label"),
            hint: t("create.brand.hint"),
            Icon: Plus,
            keywords: ["new", "catalog"],
            perform: go("/catalog/brands?action=create"),
            perm: "Permissions.Catalog.Brands.Create",
          },
          {
            id: "create-category",
            label: t("create.category.label"),
            hint: t("create.category.hint"),
            Icon: Plus,
            keywords: ["new", "catalog"],
            perform: go("/catalog/categories?action=create"),
            perm: "Permissions.Catalog.Categories.Create",
          },
          {
            id: "create-ticket",
            label: t("create.ticket.label"),
            hint: t("create.ticket.hint"),
            Icon: Plus,
            keywords: ["new", "support", "issue"],
            perform: go("/tickets?action=create"),
            perm: "Permissions.Tickets.Create",
          },
          {
            id: "create-channel",
            label: t("create.channel.label"),
            hint: t("create.channel.hint"),
            Icon: Plus,
            keywords: ["new", "chat", "channel"],
            perform: go("/chat?action=create-channel"),
            perm: "Permissions.Chat.Channels.Create",
          },
          {
            id: "create-file",
            label: t("create.file.label"),
            hint: t("create.file.hint"),
            Icon: Plus,
            keywords: ["new", "upload", "attach"],
            perform: go("/files?action=upload"),
            perm: "Permissions.Files.Upload",
          },
        ],
      },
      {
        heading: t("group.account"),
        items: [
          {
            id: "acc-profile",
            label: t("account.profile.label"),
            hint: t("account.profile.hint"),
            Icon: UserRound,
            perform: go("/settings/profile"),
          },
          {
            id: "acc-security",
            label: t("account.security.label"),
            hint: t("account.security.hint"),
            Icon: Shield,
            keywords: ["password", "2fa", "sessions"],
            perform: go("/settings/security"),
          },
          {
            id: "acc-keys",
            label: t("account.keys.label"),
            hint: t("account.keys.hint"),
            Icon: KeyRound,
            keywords: ["token", "credentials"],
            perform: go("/settings/api-keys"),
          },
          {
            id: "acc-notifications",
            label: t("account.notifications.label"),
            hint: t("account.notifications.hint"),
            Icon: Sparkles,
            perform: go("/settings/notifications"),
          },
          {
            id: "acc-appearance",
            label: t("account.appearance.label"),
            hint: t("account.appearance.hint"),
            Icon: Palette,
            keywords: ["theme", "font", "density", "dark", "light"],
            perform: go("/settings/appearance"),
          },
        ],
      },
      {
        heading: t("group.theme"),
        items: [
          {
            id: "theme-light",
            label: t("theme.light"),
            Icon: Sun,
            keywords: ["bright", "day"],
            perform: () => setMode("light"),
          },
          {
            id: "theme-dark",
            label: t("theme.dark"),
            Icon: Moon,
            keywords: ["night", "oled"],
            perform: () => setMode("dark"),
          },
          {
            id: "theme-system",
            label: t("theme.system"),
            Icon: Monitor,
            keywords: ["auto"],
            perform: () => setMode("system"),
          },
        ],
      },
      {
        heading: t("group.accent"),
        items: accents.map((a) => ({
          id: `accent-${a.id}`,
          label: t("accent.set", { name: a.label }),
          hint: a.description,
          Icon: Palette,
          keywords: ["color", "brand", a.id],
          perform: () => setAccent(a.id),
        })),
      },
      {
        heading: t("group.session"),
        items: [
          {
            id: "sess-logout",
            label: t("session.logout.label"),
            hint: t("session.logout.hint"),
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
  }, [navigate, onOpenChange, setMode, setAccent, logout, permissions, t]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={cn(
          "max-w-[640px] p-0 sm:max-w-[640px]",
          "bg-[var(--color-popover)]",
        )}
      >
        <DialogTitle className="sr-only">{t("ui.srTitle")}</DialogTitle>
        <DialogDescription className="sr-only">
          {t("ui.srDescription")}
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
              placeholder={t("ui.searchPlaceholder")}
              aria-label={t("ui.searchAria")}
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
              <p className="text-sm font-medium tracking-tight">{t("ui.emptyTitle")}</p>
              <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                {t("ui.emptyBody")}
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
                {t("ui.navigate")}
              </span>
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↵</kbd>
                {t("ui.select")}
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
