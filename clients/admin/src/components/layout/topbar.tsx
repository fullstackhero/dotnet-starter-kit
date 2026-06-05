import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Check,
  ChevronsUpDown,
  LogOut,
  Moon,
  Settings as SettingsIcon,
  Sun,
  UserRound,
} from "lucide-react";
import { MobileNavTrigger } from "@/components/layout/mobile-nav";
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
import { useAuth } from "@/auth/use-auth";
import { getMyProfile } from "@/api/users";
import { useTheme } from "@/components/theme/theme-provider";
import { cn } from "@/lib/cn";

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

/** Up-to-2-char initials from a display name. */
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

/**
 * ProfileTile — the square topbar avatar. Renders the uploaded profile
 * image when present (object-cover to fill the rounded tile) and falls
 * back to the brand-tinted initials tile when missing or on load error.
 * Keyed by `src` at the call site so a freshly-uploaded image resets the
 * failure state and shows immediately.
 */
function ProfileTile({ src, initials }: { src: string | null; initials: string }) {
  const [failed, setFailed] = useState(false);
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
          width={32}
          height={32}
          loading="lazy"
          decoding="async"
          onError={() => setFailed(true)}
          className="h-full w-full object-cover"
        />
      ) : (
        initials
      )}
    </span>
  );
}

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

// ─────────────────────────────────────────────────────────────────────────────
// Topbar
// ─────────────────────────────────────────────────────────────────────────────

export function Topbar() {
  const { user, logout } = useAuth();
  const { theme, setTheme } = useTheme();
  const navigate = useNavigate();
  const [confirmOpen, setConfirmOpen] = useState(false);

  // Avatar lives on the profile, not the JWT — read it from the shared
  // ["identity","profile"] query so it updates the instant a new image is
  // uploaded (the profile page invalidates that key on success).
  const profile = useQuery({
    queryKey: ["identity", "profile"],
    queryFn: getMyProfile,
    staleTime: 60_000,
  });
  const avatarUrl = profile.data?.imageUrl ?? null;
  const displayName = user?.name ?? user?.email ?? "Unknown";

  const onConfirmSignOut = () => {
    setConfirmOpen(false);
    logout();
  };

  return (
    <header
      className={cn(
        "sticky top-0 z-30 flex h-14 shrink-0 items-center gap-2",
        "border-b border-[var(--color-border)] bg-[oklch(from_var(--color-background)_l_c_h_/_0.8)]",
        "px-3 backdrop-blur-sm md:px-5",
      )}
    >
      {/* Mobile nav trigger — leading edge on small screens */}
      <MobileNavTrigger />

      {/* Spacer pushes right-side actions to the trailing edge */}
      <div className="flex-1" />

      {/* Notification bell */}
      <NotificationBell />

      {/* User dropdown — `modal={false}` so the sign-out confirmation
          Dialog doesn't deadlock pointer-events via nested modals. */}
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
          {/* Square avatar tile — uploaded image with initials fallback */}
          <ProfileTile
            key={avatarUrl ?? "none"}
            src={avatarUrl}
            initials={initialsOf(user?.name ?? user?.email)}
          />
          {/* Name + tenant — desktop only */}
          <div className="hidden min-w-0 text-left md:block">
            <p className="truncate text-[12px] font-medium leading-none text-[var(--color-foreground)]">
              {displayName}
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

        <DropdownMenuContent align="end" sideOffset={6} className="w-[240px] p-0">
          {/* User info header */}
          <div className="px-3 py-2.5">
            <p className="truncate text-[12px] font-semibold text-[var(--color-foreground)]">
              {displayName}
            </p>
            {user?.email && user.name && (
              <p className="mt-0.5 truncate text-[10.5px] text-[var(--color-muted-foreground)]">
                {user.email}
              </p>
            )}
            {user?.tenant && (
              <div className="mt-1.5 flex items-center gap-1.5">
                <span
                  aria-hidden
                  className="inline-flex h-1.5 w-1.5 rounded-full bg-[var(--color-success)]"
                />
                <span className="text-[10px] text-[var(--color-muted-foreground)]">
                  {user.tenant}
                </span>
              </div>
            )}
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* Theme */}
          <DropdownMenuLabel className="px-3 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Theme
          </DropdownMenuLabel>
          <div className="px-1 pb-1">
            <ThemeMenuItem
              icon={Sun}
              label="Light"
              active={theme === "light"}
              onSelect={() => setTheme("light")}
            />
            <ThemeMenuItem
              icon={Moon}
              label="Dark"
              active={theme === "dark"}
              onSelect={() => setTheme("dark")}
            />
          </div>

          <DropdownMenuSeparator className="!my-0" />

          {/* Account quick actions */}
          <DropdownMenuLabel className="px-3 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Account
          </DropdownMenuLabel>
          <div className="px-1 pb-1">
            <SimpleMenuItem
              icon={UserRound}
              label="Profile"
              onSelect={() => navigate("/settings/profile")}
            />
            <SimpleMenuItem
              icon={SettingsIcon}
              label="Settings"
              onSelect={() => navigate("/settings")}
            />
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

      {/* Sign-out confirmation dialog */}
      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Sign out of fullstackhero?</DialogTitle>
            <DialogDescription>
              You'll need to sign in again to access this admin. Any unsaved
              work in this session will be lost.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <div className="flex items-center gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2.5">
              <Avatar name={displayName} src={avatarUrl} size="md" />
              <div className="min-w-0 flex-1">
                <div className="truncate text-sm font-medium tracking-tight">
                  {displayName}
                </div>
                {user?.email && user.name && (
                  <div className="truncate text-xs text-[var(--color-muted-foreground)]">
                    {user.email}
                  </div>
                )}
              </div>
              {user?.tenant && (
                <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px]">
                  {user.tenant}
                </code>
              )}
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
