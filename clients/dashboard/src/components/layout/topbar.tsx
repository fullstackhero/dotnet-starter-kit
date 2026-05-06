import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  ChevronDown,
  ChevronRight,
  KeyRound,
  LogOut,
  Monitor,
  Moon,
  Search,
  Settings as SettingsIcon,
  Sun,
  UserRound,
} from "lucide-react";
import { useCommandPalette } from "@/components/command-palette/command-palette";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Avatar } from "@/components/ui/avatar";
import { useAuth } from "@/auth/use-auth";
import { useSse } from "@/sse/sse-context";
import { useTheme, type ThemeMode } from "@/components/theme/theme-provider";
import { cn } from "@/lib/cn";

// ─────────────────────────────────────────────────────────────────────
// Theme tile — full card with icon + label + miniature preview swatch.
// Used inside the dropdown's Theme row.
// ─────────────────────────────────────────────────────────────────────

type ThemeTileSpec = {
  value: ThemeMode;
  label: string;
  Icon: React.ComponentType<{ className?: string }>;
  /** Inline preview swatch styling — tiny mock of light / split / dark. */
  preview: React.CSSProperties;
};

const themeTiles: ThemeTileSpec[] = [
  {
    value: "light",
    label: "Light",
    Icon: Sun,
    preview: {
      background:
        "linear-gradient(135deg, oklch(0.985 0.002 270), oklch(0.928 0.006 270))",
    },
  },
  {
    value: "system",
    label: "System",
    Icon: Monitor,
    preview: {
      background:
        "linear-gradient(135deg, oklch(0.985 0.002 270) 0%, oklch(0.985 0.002 270) 49%, oklch(0.165 0.011 270) 51%, oklch(0.165 0.011 270) 100%)",
    },
  },
  {
    value: "dark",
    label: "Dark",
    Icon: Moon,
    preview: {
      background:
        "linear-gradient(135deg, oklch(0.165 0.011 270), oklch(0.080 0.009 270))",
    },
  },
];

function ThemeTile({
  spec,
  active,
  onClick,
}: {
  spec: ThemeTileSpec;
  active: boolean;
  onClick: () => void;
}) {
  const { Icon, label } = spec;
  return (
    <button
      type="button"
      onClick={onClick}
      role="radio"
      aria-checked={active}
      title={label}
      className={cn(
        "group/tile relative flex cursor-pointer flex-col items-center gap-1.5 overflow-hidden rounded-lg border p-2.5",
        "transition-colors duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        active
          ? "border-[var(--color-primary)] bg-[var(--color-primary-soft)]"
          : "border-[var(--color-border)] bg-[var(--color-surface-2)] hover:border-[var(--color-border-strong)]",
      )}
    >
      <span
        aria-hidden
        className="grid h-9 w-full place-items-center rounded-md ring-1 ring-inset ring-[oklch(from_var(--color-foreground)_l_c_h_/_0.05)]"
        style={spec.preview}
      >
        <Icon
          className={cn(
            "h-4 w-4 drop-shadow-[0_1px_1px_oklch(0_0_0_/_0.20)]",
            spec.value === "dark" ? "text-white/80" : "text-[oklch(0.265_0.012_270)]",
          )}
        />
      </span>
      <span
        className={cn(
          "text-[11px] font-medium tracking-tight",
          active ? "text-[var(--color-primary)]" : "text-[var(--color-foreground)]",
        )}
      >
        {label}
      </span>
    </button>
  );
}

// ─────────────────────────────────────────────────────────────────────
// Quick-action item — icon in a brand-soft chip + label + chevron.
// Reads as a command, not a passive list row.
// ─────────────────────────────────────────────────────────────────────

function QuickAction({
  icon: Icon,
  label,
  description,
  onSelect,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  description?: string;
  onSelect: () => void;
}) {
  return (
    <DropdownMenuItem
      onSelect={onSelect}
      className="!my-0 flex items-center gap-3 rounded-lg !px-2.5 !py-2"
    >
      <span
        aria-hidden
        className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)] transition-colors group-data-[highlighted]:bg-[var(--color-primary-soft)] group-data-[highlighted]:text-[var(--color-primary)]"
      >
        <Icon className="h-3.5 w-3.5" />
      </span>
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="text-sm font-medium tracking-tight">{label}</span>
        {description && (
          <span className="text-[11px] text-[var(--color-muted-foreground)]">
            {description}
          </span>
        )}
      </span>
      <ChevronRight
        className="h-3.5 w-3.5 shrink-0 text-[var(--color-muted-foreground)] transition-transform group-data-[highlighted]:translate-x-0.5"
        aria-hidden
      />
    </DropdownMenuItem>
  );
}

// ─────────────────────────────────────────────────────────────────────
// Topbar
// ─────────────────────────────────────────────────────────────────────

export function Topbar() {
  const { user, logout } = useAuth();
  const { status: sseStatus, eventCount } = useSse();
  const { mode, setMode } = useTheme();
  const { setOpen: setPaletteOpen } = useCommandPalette();
  const navigate = useNavigate();
  const [confirmOpen, setConfirmOpen] = useState(false);

  const onConfirmSignOut = () => {
    setConfirmOpen(false);
    logout();
  };

  const avatarStatus =
    sseStatus === "connected"
      ? ("online" as const)
      : sseStatus === "error"
        ? ("warning" as const)
        : sseStatus === "idle"
          ? undefined
          : ("offline" as const);

  const presence = (() => {
    if (sseStatus === "connected") {
      return {
        color: "var(--color-success)",
        text: `Connected · ${new Intl.NumberFormat("en-US").format(eventCount)} events`,
      };
    }
    if (sseStatus === "error") {
      return { color: "var(--color-destructive)", text: "Stream offline" };
    }
    if (sseStatus === "connecting") {
      return { color: "var(--color-muted-foreground)", text: "Connecting…" };
    }
    if (sseStatus === "reconnecting") {
      return { color: "var(--color-warning)", text: "Reconnecting…" };
    }
    return { color: "var(--color-muted-foreground)", text: "Idle" };
  })();

  return (
    <header
      className={cn(
        "sticky top-0 z-30 flex h-14 shrink-0 items-center justify-end gap-2",
        "border-b border-[var(--color-border)] bg-[oklch(from_var(--color-surface-1)_l_c_h_/_0.72)]",
        "px-4 backdrop-blur-xl backdrop-saturate-150",
      )}
    >
      {/* Command palette trigger — opens via ⌘K from anywhere; the
          chip in the topbar is a discoverability affordance. */}
      <button
        type="button"
        onClick={() => setPaletteOpen(true)}
        title="Open command palette"
        className={cn(
          "hidden h-8 cursor-pointer items-center gap-2 rounded-md bg-[var(--color-surface-2)] px-2.5 text-xs",
          "gradient-border text-[var(--color-muted-foreground)]",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          "md:inline-flex",
        )}
      >
        <Search className="h-3.5 w-3.5" />
        <span>Search</span>
        <kbd className="ml-2 rounded border border-[var(--color-border-strong)] bg-[var(--color-surface-1)] px-1.5 py-px font-mono text-[10px] font-medium tracking-tight">
          ⌘K
        </kbd>
      </button>

      {/* `modal={false}` is required because we open the sign-out
          confirmation Dialog from a DropdownMenuItem. Default modal mode
          locks pointer-events on the body, which can leave the page
          unclickable after the dialog closes due to a Radix lifecycle
          race between the two overlays. Non-modal still auto-closes on
          outside click and keeps keyboard navigation. */}
      <DropdownMenu modal={false}>
        <DropdownMenuTrigger asChild>
          <button
            type="button"
            aria-label="Open profile menu"
            className={cn(
              "group/profile flex cursor-pointer items-center gap-2 rounded-full py-1 pl-1 pr-2",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "hover:bg-[var(--color-accent)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              "data-[state=open]:bg-[var(--color-accent)]",
            )}
          >
            <Avatar name={user?.name ?? user?.email ?? "?"} status={avatarStatus} size="sm" />
            <ChevronDown
              className={cn(
                "h-3.5 w-3.5 text-[var(--color-muted-foreground)]",
                "transition-transform duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                "group-data-[state=open]/profile:rotate-180",
              )}
              aria-hidden
            />
          </button>
        </DropdownMenuTrigger>

        <DropdownMenuContent
          align="end"
          sideOffset={10}
          className="w-[340px] p-0"
        >
          {/* ── Hero ────────────────────────────────────────────────
             Atmospheric panel: brand-tinted radial mesh + soft inset
             highlight. Avatar floats in halo. Status row anchors at
             the bottom. */}
          <div className="relative overflow-hidden">
            {/* Brand glow — corner radial + diagonal counter-tint. */}
            <div
              aria-hidden
              className="pointer-events-none absolute inset-0"
              style={{
                background:
                  "radial-gradient(ellipse 70% 50% at 0% 0%, oklch(from var(--color-primary) l c h / 0.18), transparent 60%), radial-gradient(ellipse 60% 50% at 100% 0%, oklch(0.700 0.155 195 / 0.10), transparent 60%)",
              }}
            />
            <div className="relative px-5 pb-4 pt-5">
              <div className="flex items-start gap-4">
                <Avatar
                  name={user?.name ?? user?.email ?? "?"}
                  status={avatarStatus}
                  size="lg"
                  halo
                />
                <div className="min-w-0 flex-1">
                  <div className="text-display truncate text-[15px] font-semibold tracking-tight leading-tight">
                    {user?.name ?? user?.email ?? "Unknown"}
                  </div>
                  {user?.email && user.name && (
                    <div className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                      {user.email}
                    </div>
                  )}
                  <div className="mt-2 flex items-center gap-1.5">
                    <span className="font-mono text-[10px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
                      Tenant
                    </span>
                    <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[10.5px] font-medium text-[var(--color-primary)]">
                      {user?.tenant ?? "—"}
                    </code>
                  </div>
                </div>
              </div>

              {/* Live presence row — pulse dot in current status hue,
                  mono "Connected · N events" line. */}
              <div className="mt-4 flex items-center gap-2 rounded-md border border-[var(--color-border)] bg-[oklch(from_var(--color-surface-2)_l_c_h_/_0.7)] px-2.5 py-1.5">
                <span
                  aria-hidden
                  className={cn(
                    "inline-flex h-1.5 w-1.5 rounded-full",
                    sseStatus === "connected" && "pulse-dot",
                  )}
                  style={{ backgroundColor: presence.color, color: presence.color }}
                />
                <span className="font-mono text-[11px] tracking-tight text-[var(--color-foreground)]">
                  {presence.text}
                </span>
              </div>
            </div>
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* ── Theme — three preview tiles ───────────────────────── */}
          <DropdownMenuLabel>Theme</DropdownMenuLabel>
          <div role="radiogroup" aria-label="Theme" className="grid grid-cols-3 gap-1.5 px-3 pb-3">
            {themeTiles.map((spec) => (
              <ThemeTile
                key={spec.value}
                spec={spec}
                active={mode === spec.value}
                onClick={() => setMode(spec.value)}
              />
            ))}
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* ── Quick actions ─────────────────────────────────────── */}
          <DropdownMenuLabel>Account</DropdownMenuLabel>
          <div className="flex flex-col gap-0.5 px-1.5 pb-2">
            <QuickAction
              icon={UserRound}
              label="Profile"
              description="Name, email, contact"
              onSelect={() => navigate("/settings/profile")}
            />
            <QuickAction
              icon={SettingsIcon}
              label="Settings"
              description="Preferences and tenant"
              onSelect={() => navigate("/settings")}
            />
            <QuickAction
              icon={KeyRound}
              label="API keys"
              description="Generate and rotate"
              onSelect={() => navigate("/settings/api-keys")}
            />
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* ── Sign out (destructive) ────────────────────────────── */}
          <div className="px-1.5 py-1.5">
            <DropdownMenuItem
              destructive
              onSelect={() => setConfirmOpen(true)}
              className="!my-0 rounded-lg !px-2.5 !py-2"
            >
              <span
                aria-hidden
                className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] text-[var(--color-destructive)]"
              >
                <LogOut className="h-3.5 w-3.5" />
              </span>
              <span className="flex-1 text-sm font-medium tracking-tight">Sign out</span>
              <kbd className="rounded border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-1.5 py-px font-mono text-[10px] tracking-tight text-[var(--color-muted-foreground)]">
                ⌘⇧Q
              </kbd>
            </DropdownMenuItem>
          </div>

          {/* ── Footer ────────────────────────────────────────────── */}
          <div className="flex items-center justify-between border-t border-[var(--color-border)] px-4 py-2.5">
            <p className="font-mono text-[10px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              v0.1 · console
            </p>
            <a
              href="https://fullstackhero.net"
              target="_blank"
              rel="noreferrer"
              className="font-mono text-[10px] tracking-tight text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
            >
              fullstackhero.net ↗
            </a>
          </div>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Sign-out confirmation dialog. */}
      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Sign out of FullStackHero?</DialogTitle>
            <DialogDescription>
              You'll need to sign in again to access this tenant. Any unsaved
              work in this session will be lost.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <div className="flex items-center gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-3 py-2.5">
              <Avatar name={user?.name ?? user?.email ?? "?"} size="md" />
              <div className="min-w-0 flex-1">
                <div className="truncate text-sm font-medium tracking-tight">
                  {user?.name ?? user?.email ?? "Unknown"}
                </div>
                {user?.email && user.name && (
                  <div className="truncate text-xs text-[var(--color-muted-foreground)]">
                    {user.email}
                  </div>
                )}
              </div>
              <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px]">
                {user?.tenant ?? "—"}
              </code>
            </div>
          </DialogBody>
          <DialogFooter>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setConfirmOpen(false)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              size="sm"
              onClick={onConfirmSignOut}
              autoFocus
            >
              <LogOut className="mr-1.5 h-3.5 w-3.5" />
              Sign out
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </header>
  );
}
