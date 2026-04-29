import { LogOut, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
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
        <Button variant="outline" size="sm" onClick={logout}>
          <LogOut className="mr-2 h-3.5 w-3.5" />
          Sign out
        </Button>
      </div>
    </header>
  );
}
