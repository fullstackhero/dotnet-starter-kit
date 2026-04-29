import { NavLink } from "react-router-dom";
import { LayoutDashboard, Activity, Receipt } from "lucide-react";
import { cn } from "@/lib/cn";

type NavItem = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
};

const items: NavItem[] = [
  { to: "/", label: "Overview", icon: LayoutDashboard },
  { to: "/activity", label: "Live activity", icon: Activity },
  { to: "/invoices", label: "Invoices", icon: Receipt },
];

export function Sidebar() {
  return (
    <aside
      className={cn(
        "hidden w-60 shrink-0 flex-col border-r border-[var(--color-border)]",
        "bg-[var(--color-surface-2)] md:flex",
      )}
    >
      {/* Brand mark — animated conic gradient under the "F" glyph. */}
      <div className="flex h-14 items-center gap-2.5 border-b border-[var(--color-border)] px-5">
        <span
          aria-hidden
          className={cn(
            "brand-mark grid h-7 w-7 place-items-center rounded-md",
            "text-[11px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
            "shadow-[0_1px_0_oklch(1_0_0_/_0.20)_inset,0_4px_14px_-4px_oklch(from_var(--color-primary)_l_c_h_/_0.55)]",
          )}
        >
          F
        </span>
        <span className="font-semibold tracking-tight">FullStackHero</span>
      </div>

      <nav className="flex-1 space-y-0.5 p-3">
        {items.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === "/"}
            className={({ isActive }) =>
              cn(
                "group relative flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium",
                "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                isActive
                  ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              )
            }
          >
            {({ isActive }) => (
              <>
                {/* 2px brand bar that slides in on the active item.
                    Sits flush to the rounded edge. */}
                <span
                  aria-hidden
                  className={cn(
                    "absolute left-0 top-1/2 h-4 w-0.5 -translate-y-1/2 rounded-r-full bg-[var(--color-primary)]",
                    "transition-opacity duration-[var(--duration-default)]",
                    isActive ? "opacity-100" : "opacity-0",
                  )}
                />
                <Icon className="h-4 w-4 shrink-0" />
                <span>{label}</span>
              </>
            )}
          </NavLink>
        ))}
      </nav>

      {/* Footer micro-copy — restrained, sets a tone of "we shipped this carefully". */}
      <div className="border-t border-[var(--color-border)] px-5 py-3">
        <p className="font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
          v0.1 · console
        </p>
      </div>
    </aside>
  );
}
