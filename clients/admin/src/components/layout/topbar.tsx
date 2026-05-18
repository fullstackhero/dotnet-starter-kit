import { Link, useLocation } from "react-router-dom";
import { LogOut, Settings } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ThemeToggle } from "@/components/theme/theme-toggle";
import { MobileNav } from "@/components/layout/mobile-nav";
import { NotificationBell } from "@/components/notifications/notification-bell";
import { useAuth } from "@/auth/use-auth";

/**
 * Topbar — section breadcrumb + tenant indicator + theme toggle + account.
 *
 * The breadcrumb is derived from the URL so it tracks deep routes
 * (/billing/invoices/123 → BILLING / INVOICES / 123) without needing route
 * declarations to carry display metadata. The tenant indicator has a
 * blinking caret because this is, after all, a console.
 */
export function Topbar() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const crumbs = breadcrumbFromPath(location.pathname);

  return (
    <header className="relative flex h-14 shrink-0 items-center justify-between gap-4 border-b border-[var(--color-border)] bg-[var(--color-background)] px-4 md:px-6">
      {/* Left — mobile menu trigger + section breadcrumb */}
      <div className="flex min-w-0 items-center gap-3">
        <MobileNav />
        <span className="meta hidden text-[var(--color-muted-foreground)] sm:inline">{"//"}</span>
        <ol className="flex min-w-0 items-center gap-1.5 truncate font-mono text-[11px] uppercase tracking-[var(--tracking-meta)]">
          {crumbs.map((crumb, i) => {
            const last = i === crumbs.length - 1;
            return (
              <li key={`${crumb}-${i}`} className="flex shrink-0 items-center gap-1.5">
                <span
                  className={
                    last
                      ? "text-[var(--color-foreground)]"
                      : "text-[var(--color-muted-foreground)]"
                  }
                >
                  {crumb}
                </span>
                {!last && (
                  <span className="text-[var(--color-muted-foreground)]/60" aria-hidden>
                    /
                  </span>
                )}
              </li>
            );
          })}
        </ol>
      </div>

      {/* Right — tenant indicator + theme toggle + account */}
      <div className="flex items-center gap-4">
        <TenantIndicator tenant={user?.tenant} />
        <div className="hidden text-right md:block">
          <div className="text-sm font-medium leading-tight">
            {user?.name ?? user?.email ?? "Unknown"}
          </div>
          {user?.email && user.name && (
            <div className="font-mono text-[10.5px] text-[var(--color-muted-foreground)] leading-tight">
              {user.email}
            </div>
          )}
        </div>
        <div className="flex items-center gap-1.5">
          <NotificationBell />
          <Button asChild variant="ghost" size="icon" aria-label="Settings">
            <Link to="/settings/profile">
              <Settings className="h-4 w-4" />
            </Link>
          </Button>
          <ThemeToggle />
          <Button variant="ghost" size="sm" onClick={logout} aria-label="Sign out">
            <LogOut className="h-4 w-4" />
            <span className="sr-only md:not-sr-only">Sign out</span>
          </Button>
        </div>
      </div>
    </header>
  );
}

function TenantIndicator({ tenant }: { tenant?: string }) {
  return (
    <div
      className="hidden items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2.5 py-1 md:inline-flex"
      title="Active tenant"
    >
      <span className="pulse-dot" aria-hidden />
      <span className="meta text-[var(--color-muted-foreground)]">tenant</span>
      <span className="font-mono text-[12px] font-medium tracking-tight text-[var(--color-foreground)]">
        {tenant ?? "—"}
        <span className="caret text-[var(--color-accent-signal)]" aria-hidden />
      </span>
    </div>
  );
}

// ─── helpers ─────────────────────────────────────────────────────────

function breadcrumbFromPath(pathname: string): string[] {
  if (pathname === "/" || pathname === "") return ["OVERVIEW"];
  return pathname
    .split("/")
    .filter(Boolean)
    .map((segment) => decodeURIComponent(segment).toUpperCase());
}
