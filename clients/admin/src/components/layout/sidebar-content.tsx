import { useMemo } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { BrandMark } from "@/components/brand-mark";
import { NAV_ITEMS, filterNavItems, isNavItemActive } from "@/components/layout/nav-items";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";

type SidebarContentProps = {
  /** Optional click hook — used by the mobile drawer to close on navigate. */
  onNavigate?: () => void;
};

/**
 * SidebarContent — the inner nav layout shared between the desktop Sidebar
 * and the mobile drawer. Magazine TOC numbering (01, 02, …), chartreuse
 * active-rail on the left of the selected entry, mono footer kicker.
 */
export function SidebarContent({ onNavigate }: SidebarContentProps) {
  const location = useLocation();
  const { user, permissionsHydrated } = useAuth();
  // Render the unfiltered list while permissions are still loading so the
  // sidebar doesn't flash empty on cold-start. Filter once they're known.
  const items = useMemo(() => {
    if (!permissionsHydrated) return NAV_ITEMS;
    return filterNavItems(NAV_ITEMS, user?.permissions ?? []);
  }, [permissionsHydrated, user?.permissions]);
  return (
    <div className="flex h-full flex-col">
      {/* Brand block */}
      <div className="flex h-14 items-center border-b border-[var(--color-border)] px-5">
        <BrandMark />
      </div>

      {/* Section marker */}
      <div className="px-5 pt-5">
        <div className="meta text-[var(--color-muted-foreground)]">// Navigation</div>
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto px-3 py-3" aria-label="Primary">
        <ul className="space-y-0.5">
          {items.map((item, idx) => {
            const isActive = isNavItemActive(item, location.pathname);
            const indexLabel = String(idx + 1).padStart(2, "0");
            const Icon = item.icon;
            return (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.to === "/"}
                  onClick={onNavigate}
                  className={cn(
                    "group relative flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-[var(--color-accent)] text-[var(--color-foreground)]"
                      : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                  )}
                >
                  <span
                    aria-hidden
                    className={cn(
                      "absolute left-0 top-1.5 bottom-1.5 w-[3px] rounded-r-sm transition-opacity",
                      isActive
                        ? "bg-[var(--color-accent-signal)] opacity-100"
                        : "bg-[var(--color-accent-signal)] opacity-0",
                    )}
                  />
                  <span className="font-mono text-[10.5px] font-medium tracking-tight text-[var(--color-muted-foreground)] tabular-nums">
                    {indexLabel}
                  </span>
                  <Icon
                    className={cn(
                      "h-4 w-4 transition-colors",
                      isActive
                        ? "text-[var(--color-foreground)]"
                        : "text-[var(--color-muted-foreground)] group-hover:text-[var(--color-foreground)]",
                    )}
                  />
                  <span className="font-medium">{item.label}</span>
                </NavLink>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* Footer credit */}
      <div className="border-t border-[var(--color-border)] px-5 py-4">
        <div className="meta text-[var(--color-muted-foreground)]">v0.1 · console</div>
        <div className="mt-1 font-mono text-[10.5px] text-[var(--color-muted-foreground)] leading-relaxed">
          platform · administration · interface
        </div>
      </div>
    </div>
  );
}
