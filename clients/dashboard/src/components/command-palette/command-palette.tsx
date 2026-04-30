import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useNavigate } from "react-router-dom";
import { Command } from "cmdk";
import {
  Activity,
  HeartPulse,
  KeyRound,
  LayoutDashboard,
  LogOut,
  Monitor,
  Moon,
  Palette,
  Receipt,
  ScrollText,
  Search,
  Settings as SettingsIcon,
  Shield,
  Sparkles,
  Sun,
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
import { cn } from "@/lib/cn";

type CommandPaletteContextValue = {
  open: boolean;
  setOpen: (next: boolean) => void;
  toggle: () => void;
};

const CommandPaletteContext = createContext<CommandPaletteContextValue | null>(null);

export function useCommandPalette() {
  const ctx = useContext(CommandPaletteContext);
  if (!ctx) throw new Error("useCommandPalette must be used within CommandPaletteProvider");
  return ctx;
}

/**
 * Palette provider — owns open state, registers the global ⌘K / Ctrl+K
 * listener, and renders the palette dialog. Components anywhere in the
 * tree can call useCommandPalette() to open it imperatively.
 */
export function CommandPaletteProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const toggle = useCallback(() => setOpen((o) => !o), []);

  // Global keybinding — ⌘K on macOS, Ctrl+K elsewhere.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        toggle();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [toggle]);

  const value = useMemo<CommandPaletteContextValue>(
    () => ({ open, setOpen, toggle }),
    [open, toggle],
  );

  // The dialog itself uses useNavigate, which requires the router
  // context. Since the provider is rendered as a sibling of the
  // RouterProvider (so children of every route share the open state),
  // the dialog can't live here. It's rendered separately by
  // <CommandPaletteRoot /> inside the AppShell.
  return (
    <CommandPaletteContext.Provider value={value}>
      {children}
    </CommandPaletteContext.Provider>
  );
}

/**
 * Renders the palette dialog. Must be mounted inside the React Router
 * tree so useNavigate is available. Reads open/close from the provider.
 */
export function CommandPaletteRoot() {
  const { open, setOpen } = useCommandPalette();
  return <CommandPaletteDialog open={open} onOpenChange={setOpen} />;
}

// ────────────────────────────────────────────────────────────────────
// Internal — the dialog body itself
// ────────────────────────────────────────────────────────────────────

type ActionItem = {
  id: string;
  label: string;
  hint?: string;
  Icon: React.ComponentType<{ className?: string }>;
  /** Free-form keywords for fuzzy matching. */
  keywords?: string[];
  shortcut?: string;
  perform: () => void;
};

type ActionGroup = {
  heading: string;
  items: ActionItem[];
};

function CommandPaletteDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
}) {
  const navigate = useNavigate();
  const { logout } = useAuth();
  const { setMode, setAccent } = useTheme();

  // Build the action set fresh each time the palette opens. The ones
  // that navigate close the palette; the ones that mutate appearance
  // don't, so the user can preview multiple choices.
  const groups = useMemo<ActionGroup[]>(() => {
    const close = () => onOpenChange(false);
    const go = (path: string) => () => {
      navigate(path);
      close();
    };
    return [
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
            id: "nav-invoices",
            label: "Invoices",
            hint: "Billing history",
            Icon: Receipt,
            keywords: ["billing", "payment"],
            perform: go("/invoices"),
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
  }, [navigate, onOpenChange, setMode, setAccent, logout]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={cn(
          "max-w-[640px] p-0 sm:max-w-[640px]",
          "bg-[oklch(from_var(--color-popover)_l_c_h_/_0.92)] backdrop-blur-2xl backdrop-saturate-150",
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
          {/* Search row */}
          <div className="flex items-center gap-2.5 border-b border-[var(--color-border)] px-4 py-3">
            <Search className="h-4 w-4 shrink-0 text-[var(--color-muted-foreground)]" aria-hidden />
            <Command.Input
              placeholder="Type a command or search…"
              className={cn(
                "h-7 flex-1 bg-transparent text-sm tracking-tight placeholder:text-[var(--color-muted-foreground)]",
                "focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
              )}
              autoFocus
            />
            <kbd className="rounded border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-1.5 py-px font-mono text-[10px] tracking-tight text-[var(--color-muted-foreground)]">
              Esc
            </kbd>
          </div>

          {/* Results */}
          <Command.List className="max-h-[420px] overflow-y-auto px-2 py-2">
            <Command.Empty className="px-4 py-12 text-center">
              <p className="text-sm font-medium tracking-tight">No matches</p>
              <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                Try a different keyword — pages, settings, theme, accent, or sign-out.
              </p>
            </Command.Empty>

            {groups.map((group) => (
              <Command.Group
                key={group.heading}
                heading={group.heading}
                className={cn(
                  // Heading text styling via cmdk's nested rendering.
                  "[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:pt-3",
                  "[&_[cmdk-group-heading]]:font-mono [&_[cmdk-group-heading]]:text-[10.5px]",
                  "[&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:uppercase",
                  "[&_[cmdk-group-heading]]:tracking-[0.12em] [&_[cmdk-group-heading]]:text-[var(--color-muted-foreground)]",
                )}
              >
                {group.items.map((item) => (
                  <CommandRow key={item.id} item={item} />
                ))}
              </Command.Group>
            ))}
          </Command.List>

          {/* Footer */}
          <div className="flex items-center justify-between border-t border-[var(--color-border)] px-4 py-2.5">
            <div className="flex items-center gap-3 font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-1 py-px text-[9px]">↑</kbd>
                <kbd className="rounded border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-1 py-px text-[9px]">↓</kbd>
                navigate
              </span>
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-1 py-px text-[9px]">↵</kbd>
                select
              </span>
            </div>
            <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
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
        "data-[selected=true]:bg-[var(--color-accent)] data-[selected=true]:text-[var(--color-foreground)]",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "grid h-7 w-7 shrink-0 place-items-center rounded-md",
          "bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)]",
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
