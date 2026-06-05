import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Check,
  ChevronsUpDown,
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
import { MobileNavTrigger } from "@/components/layout/mobile-nav";
import { ChatUnreadBadge } from "@/components/notifications/chat-unread-badge";
import { NotificationBell } from "@/components/notifications/notification-bell";
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
import { getMyProfile } from "@/api/identity";
import { useAuth } from "@/auth/use-auth";
import { useSseStatus } from "@/sse/sse-context";
import { useTheme } from "@/components/theme/theme-provider";
import { cn } from "@/lib/cn";

// ─────────────────────────────────────────────────────────────────────
// User dropdown helpers — match the dentalOS sidebar user-block pattern.
// ─────────────────────────────────────────────────────────────────────

/** Up-to-2-char initials from a display name. Drops period-trailing
 *  abbreviations like "Dr." so "Dr. Mukesh Murugan" → "MM" not "DM". */
function initialsOf(name?: string | null): string {
  if (!name) return "U";
  const parts = name
    .split(/\s+/)
    .filter((w) => w.length > 0 && !w.endsWith("."));
  if (parts.length === 0) return name[0]?.toUpperCase() ?? "U";
  return parts
    .map((w) => w[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

/** Square rose-tinted user tile (dos pattern) — shows the profile photo when set,
 *  falling back to initials when there's no image or it fails to load. */
function SquareUserAvatar({ src, name }: { src?: string | null; name?: string | null }) {
  const [failed, setFailed] = useState(false);
  useEffect(() => setFailed(false), [src]);
  const showImage = Boolean(src) && !failed;
  return (
    <span
      aria-hidden
      className={cn(
        "grid size-8 shrink-0 place-items-center overflow-hidden rounded-lg",
        "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.15)] text-[var(--color-primary)]",
        "font-display text-[11px] font-bold tracking-tight",
      )}
    >
      {showImage ? (
        <img
          src={src ?? undefined}
          alt=""
          onError={() => setFailed(true)}
          className="size-full object-cover"
        />
      ) : (
        initialsOf(name)
      )}
    </span>
  );
}

/** Theme-pick row inside the dropdown — icon + label + check on the
 *  active mode. Mirrors dentalOS's "Light mode / Dark mode" items. */
function ThemeMenuItem({
  icon: Icon,
  label,
  active,
  onSelect,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  active: boolean;
  onSelect: () => void;
}) {
  return (
    <DropdownMenuItem
      onSelect={onSelect}
      className="!my-0 flex cursor-pointer items-center gap-2.5 rounded-md !px-2.5 !py-1.5"
    >
      <Icon className="size-3.5 shrink-0 text-[var(--color-muted-foreground)]" />
      <span className="flex-1 text-[12.5px] font-medium text-[var(--color-foreground)]">
        {label}
      </span>
      {active && (
        <Check className="size-3.5 shrink-0 text-[var(--color-primary)]" aria-hidden />
      )}
    </DropdownMenuItem>
  );
}

/** Simple icon + label menu item — used by the Account quick links. */
function SimpleMenuItem({
  icon: Icon,
  label,
  onSelect,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  onSelect: () => void;
}) {
  return (
    <DropdownMenuItem
      onSelect={onSelect}
      className="!my-0 flex cursor-pointer items-center gap-2.5 rounded-md !px-2.5 !py-1.5"
    >
      <Icon className="size-3.5 shrink-0 text-[var(--color-muted-foreground)]" />
      <span className="text-[12.5px] font-medium text-[var(--color-foreground)]">
        {label}
      </span>
    </DropdownMenuItem>
  );
}

// ─────────────────────────────────────────────────────────────────────
// Topbar
// ─────────────────────────────────────────────────────────────────────

export function Topbar() {
  const { user, logout } = useAuth();
  // Shared with the Profile settings page (same query key), so changing the
  // photo there invalidates this and the topbar avatar updates live.
  const { data: profile } = useQuery({
    queryKey: ["identity", "me"],
    queryFn: getMyProfile,
    staleTime: 5 * 60 * 1000,
  });
  const avatarUrl = profile?.imageUrl ?? null;
  const { status: sseStatus, eventCount } = useSseStatus();
  const { mode, setMode } = useTheme();
  const { setOpen: setPaletteOpen } = useCommandPalette();
  const navigate = useNavigate();
  const [confirmOpen, setConfirmOpen] = useState(false);

  const onConfirmSignOut = () => {
    setConfirmOpen(false);
    logout();
  };

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
        "sticky top-0 z-30 flex h-12 shrink-0 items-center gap-2",
        "border-b border-[var(--color-border)] bg-[oklch(from_var(--color-background)_l_c_h_/_0.8)]",
        "px-3 backdrop-blur-sm md:px-5",
      )}
    >
      {/* Mobile nav trigger — leading edge on small screens, hidden
          on md+ where the desktop sidebar is always visible. */}
      <MobileNavTrigger />

      {/* Spacer pushes the rest of the topbar (search + profile) to
          the trailing edge on every viewport. */}
      <div className="flex-1" />

      {/* Mobile search button — palette is reachable via icon since
          the desktop search chip is hidden below md. */}
      <button
        type="button"
        onClick={() => setPaletteOpen(true)}
        aria-label="Open command palette"
        className={cn(
          "grid h-9 w-9 cursor-pointer place-items-center rounded-md md:hidden",
          "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        )}
      >
        <Search className="h-4 w-4" aria-hidden />
      </button>

      {/* Command palette trigger — opens via ⌘K from anywhere; the
          chip in the topbar is a discoverability affordance. */}
      <button
        type="button"
        onClick={() => setPaletteOpen(true)}
        title="Open command palette"
        className={cn(
          "hidden h-8 cursor-pointer items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2.5 text-xs",
          "text-[var(--color-muted-foreground)]",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          "md:inline-flex",
        )}
      >
        <Search className="h-3.5 w-3.5" />
        <span>Search</span>
        <kbd className="ml-2 rounded border border-[var(--color-border)] bg-[var(--color-card)] px-1.5 py-px font-mono text-[10px] font-medium tracking-tight">
          ⌘K
        </kbd>
      </button>

      {/* Chat unread badge — sums unreadCount across the user's channels.
          Brand-primary chip to distinguish from the destructive-red bell. */}
      <ChatUnreadBadge />

      {/* Notification bell — bell badge + dropdown inbox. */}
      <NotificationBell />

      {/* `modal={false}` is required because we open the sign-out
          confirmation Dialog from a DropdownMenuItem. Default modal mode
          locks pointer-events on the body, which can leave the page
          unclickable after the dialog closes due to a Radix lifecycle
          race between the two overlays. Non-modal still auto-closes on
          outside click and keeps keyboard navigation. */}
      {/* User dropdown — dos pattern: small avatar tile + name + role +
          chevrons-up-down. modal={false} so the sign-out confirmation
          Dialog opening from a menu item doesn't deadlock the body
          pointer-events. */}
      <DropdownMenu modal={false}>
        <DropdownMenuTrigger asChild>
          <button
            type="button"
            aria-label="Open profile menu"
            className={cn(
              "group flex cursor-pointer items-center gap-2.5 rounded-lg py-1 pl-1 pr-2 outline-none",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "hover:bg-[var(--color-accent)]",
              "focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              "data-[state=open]:bg-[var(--color-accent)]",
            )}
          >
            {/* Rose-tinted square avatar tile — photo if set, else initials. */}
            <SquareUserAvatar src={avatarUrl} name={user?.name ?? user?.email} />
            {/* Name + role caption — desktop only */}
            <div className="hidden min-w-0 text-left md:block">
              <p className="truncate text-[12px] font-medium leading-none text-[var(--color-foreground)]">
                {user?.name ?? user?.email ?? "Unknown"}
              </p>
              <p className="mt-1 truncate text-[10px] leading-none text-[var(--color-muted-foreground)]">
                {user?.tenant ?? "—"}
              </p>
            </div>
            <ChevronsUpDown
              className={cn(
                "size-3.5 shrink-0 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]",
                "transition-colors duration-[var(--duration-fast)]",
                "group-hover:text-[var(--color-muted-foreground)]",
              )}
              aria-hidden
            />
          </button>
        </DropdownMenuTrigger>

        <DropdownMenuContent
          align="end"
          sideOffset={6}
          className="w-[240px] p-0"
        >
          {/* User info header — name + email, plain warm-paper */}
          <div className="px-3 py-2.5">
            <p className="truncate text-[12px] font-semibold text-[var(--color-foreground)]">
              {user?.name ?? user?.email ?? "Unknown"}
            </p>
            {user?.email && user.name && (
              <p className="mt-0.5 truncate text-[10.5px] text-[var(--color-muted-foreground)]">
                {user.email}
              </p>
            )}
            <div className="mt-1.5 flex items-center gap-1.5">
              <span
                aria-hidden
                className={cn(
                  "inline-flex size-1.5 rounded-full",
                  sseStatus === "connected" && "pulse-dot",
                )}
                style={{ backgroundColor: presence.color, color: presence.color }}
              />
              <span className="text-[10px] text-[var(--color-muted-foreground)]">
                {presence.text}
              </span>
            </div>
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* Theme — three simple menu items with a check on the active one */}
          <DropdownMenuLabel className="px-3 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Theme
          </DropdownMenuLabel>
          <div className="px-1 pb-1">
            <ThemeMenuItem icon={Sun} label="Light" active={mode === "light"} onSelect={() => setMode("light")} />
            <ThemeMenuItem icon={Moon} label="Dark" active={mode === "dark"} onSelect={() => setMode("dark")} />
            <ThemeMenuItem icon={Monitor} label="System" active={mode === "system"} onSelect={() => setMode("system")} />
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* Account quick actions */}
          <DropdownMenuLabel className="px-3 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Account
          </DropdownMenuLabel>
          <div className="px-1 pb-1">
            <SimpleMenuItem icon={UserRound} label="Profile" onSelect={() => navigate("/settings/profile")} />
            <SimpleMenuItem icon={SettingsIcon} label="Settings" onSelect={() => navigate("/settings")} />
            <SimpleMenuItem icon={KeyRound} label="API keys" onSelect={() => navigate("/settings/api-keys")} />
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* Sign out */}
          <div className="px-1 py-1">
            <DropdownMenuItem
              destructive
              onSelect={() => setConfirmOpen(true)}
              className="!my-0 cursor-pointer rounded-md !px-2.5 !py-1.5"
            >
              <LogOut className="size-3.5" />
              <span className="text-[12.5px] font-medium">Sign out</span>
            </DropdownMenuItem>
          </div>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Sign-out confirmation dialog. */}
      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Sign out of fullstackhero?</DialogTitle>
            <DialogDescription>
              You'll need to sign in again to access this tenant. Any unsaved
              work in this session will be lost.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <div className="flex items-center gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2.5">
              <Avatar name={user?.name ?? user?.email ?? "?"} src={avatarUrl} size="md" />
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
