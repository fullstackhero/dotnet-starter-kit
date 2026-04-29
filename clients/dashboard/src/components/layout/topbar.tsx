import { useState } from "react";
import { LogOut, Search } from "lucide-react";
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
import { useAuth } from "@/auth/use-auth";
import { SseStatusBadge } from "@/components/sse/sse-status-badge";
import { ThemeToggle } from "@/components/theme/theme-toggle";

/**
 * Cmd-K placeholder — decorative for now, sized like a real search field.
 * Plants a hook for a future command palette without committing to one.
 */
function CommandSlot() {
  return (
    <button
      type="button"
      title="Command palette (coming soon)"
      className="gradient-border hidden h-8 items-center gap-2 rounded-md bg-[var(--color-surface-2)] px-2.5 text-xs text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)] md:inline-flex"
    >
      <Search className="h-3.5 w-3.5" />
      <span>Search</span>
      <kbd className="ml-2 rounded border border-[var(--color-border-strong)] bg-[var(--color-surface-1)] px-1.5 py-px font-mono text-[10px] font-medium tracking-tight">
        ⌘K
      </kbd>
    </button>
  );
}

export function Topbar() {
  const { user, logout } = useAuth();
  const [confirmOpen, setConfirmOpen] = useState(false);

  const onConfirmSignOut = () => {
    setConfirmOpen(false);
    logout();
  };

  return (
    <header
      className={[
        "sticky top-0 z-30 flex h-14 shrink-0 items-center justify-between gap-4",
        "border-b border-[var(--color-border)] bg-[oklch(from_var(--color-surface-1)_l_c_h_/_0.72)]",
        "px-6 backdrop-blur-xl backdrop-saturate-150",
      ].join(" ")}
    >
      <div className="flex items-center gap-3">
        <SseStatusBadge />
        <span className="text-sm text-[var(--color-muted-foreground)]">
          Tenant
          <span className="ml-1.5 rounded-md bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[12px] font-medium text-[var(--color-primary)]">
            {user?.tenant ?? "—"}
          </span>
        </span>
      </div>

      <div className="flex items-center gap-3">
        <CommandSlot />
        <ThemeToggle />
        <div className="hidden text-right sm:block">
          <div className="text-sm font-medium tracking-tight">
            {user?.name ?? user?.email ?? "Unknown"}
          </div>
          {user?.email && user.name && (
            <div className="text-xs text-[var(--color-muted-foreground)]">{user.email}</div>
          )}
        </div>
        <Button variant="outline" size="sm" onClick={() => setConfirmOpen(true)}>
          <LogOut className="mr-2 h-3.5 w-3.5" />
          Sign out
        </Button>
      </div>

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
              <span
                aria-hidden
                className="grid h-7 w-7 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[11px] font-semibold uppercase text-[var(--color-primary)]"
              >
                {(user?.name ?? user?.email ?? "?").charAt(0)}
              </span>
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
