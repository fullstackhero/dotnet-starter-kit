import { useCallback, useState } from "react";
import { NavLink } from "react-router-dom";
import {
  Activity,
  LayoutDashboard,
  PanelLeftClose,
  PanelLeftOpen,
  Receipt,
  Settings,
} from "lucide-react";
import { cn } from "@/lib/cn";

const COLLAPSED_KEY = "fsh.sidebar.collapsed";

type NavSpec = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
};

type NavGroup = {
  caption: string;
  items: NavSpec[];
};

const groups: NavGroup[] = [
  {
    caption: "Operations",
    items: [
      { to: "/", label: "Overview", icon: LayoutDashboard },
      { to: "/activity", label: "Live activity", icon: Activity },
      { to: "/invoices", label: "Invoices", icon: Receipt },
    ],
  },
  {
    caption: "Account",
    items: [
      { to: "/settings", label: "Settings", icon: Settings },
    ],
  },
];

/** Persisted collapsed state. Reads localStorage on mount; writes on change. */
function useCollapsedSidebar() {
  const [collapsed, setRaw] = useState<boolean>(() => {
    if (typeof window === "undefined") return false;
    try {
      return window.localStorage.getItem(COLLAPSED_KEY) === "true";
    } catch {
      return false;
    }
  });
  const setCollapsed = useCallback((next: boolean) => {
    setRaw(next);
    try {
      window.localStorage.setItem(COLLAPSED_KEY, String(next));
    } catch {
      /* storage unavailable */
    }
  }, []);
  return {
    collapsed,
    toggle: () => setCollapsed(!collapsed),
  };
}

export function Sidebar() {
  const { collapsed, toggle } = useCollapsedSidebar();

  return (
    <aside
      data-collapsed={collapsed || undefined}
      aria-label="Primary navigation"
      className={cn(
        "hidden shrink-0 flex-col border-r border-[var(--color-border)]",
        "bg-[var(--color-surface-2)] md:flex",
        "transition-[width] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        collapsed ? "w-[64px]" : "w-60",
      )}
    >
      {/* Brand row. When expanded, the collapse toggle sits on the right;
          when collapsed, only the brand mark is shown — the expand
          toggle moves to the footer. */}
      <div
        className={cn(
          "flex h-14 shrink-0 items-center border-b border-[var(--color-border)]",
          collapsed ? "justify-center px-0" : "justify-between px-3",
        )}
      >
        <div className="flex items-center gap-2.5">
          <span
            aria-hidden
            className={cn(
              "brand-mark grid h-7 w-7 place-items-center rounded-md",
              "text-[11px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
              "shadow-[0_1px_0_oklch(1_0_0_/_0.18)_inset,0_4px_14px_-4px_oklch(from_var(--color-primary)_l_c_h_/_0.45)]",
            )}
          >
            F
          </span>
          {!collapsed && (
            <span className="whitespace-nowrap font-semibold tracking-tight">
              FullStackHero
            </span>
          )}
        </div>

        {!collapsed && (
          <button
            type="button"
            onClick={toggle}
            aria-label="Collapse sidebar"
            aria-expanded
            title="Collapse sidebar"
            className={cn(
              "grid h-7 w-7 shrink-0 place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <PanelLeftClose className="h-4 w-4" aria-hidden />
          </button>
        )}
      </div>

      {/* Nav. No overflow handling: setting `overflow-y: auto` here also
          implicitly makes overflow-x: auto, which the absolutely-
          positioned hover tooltips on collapsed nav items would trigger
          (they extend past the nav's right edge). With a small fixed
          set of nav items the column never needs to scroll, so leave
          overflow visible — the tooltips can render outside the
          sidebar without inducing a scrollbar. */}
      <nav className="flex-1 space-y-5 px-2.5 py-4">
        {groups.map((group) => (
          <NavSection key={group.caption} caption={group.caption} collapsed={collapsed}>
            {group.items.map((item) => (
              <NavItemLink key={item.to} item={item} collapsed={collapsed} />
            ))}
          </NavSection>
        ))}
      </nav>

      {/* Footer. Expanded → version chip; collapsed → expand toggle
          (so the affordance is always reachable without overcrowding
          the brand row when the column is just 64px wide). */}
      <div
        className={cn(
          "border-t border-[var(--color-border)]",
          collapsed ? "flex justify-center p-2" : "px-5 py-3",
        )}
      >
        {collapsed ? (
          <button
            type="button"
            onClick={toggle}
            aria-label="Expand sidebar"
            aria-expanded={false}
            title="Expand sidebar"
            className={cn(
              "grid h-7 w-7 shrink-0 place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <PanelLeftOpen className="h-4 w-4" aria-hidden />
          </button>
        ) : (
          <p className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            v0.1 · console
          </p>
        )}
      </div>
    </aside>
  );
}

function NavSection({
  caption,
  collapsed,
  children,
}: {
  caption: string;
  collapsed: boolean;
  children: React.ReactNode;
}) {
  return (
    <div>
      {collapsed ? (
        <div
          aria-hidden
          className="mx-2 mb-2 h-px bg-[var(--color-border)]"
          role="presentation"
        />
      ) : (
        <div className="mb-1.5 px-2.5 font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
          {caption}
        </div>
      )}
      <div className="space-y-0.5">{children}</div>
    </div>
  );
}

function NavItemLink({ item, collapsed }: { item: NavSpec; collapsed: boolean }) {
  const Icon = item.icon;
  return (
    <NavLink
      to={item.to}
      end={item.to === "/"}
      title={collapsed ? item.label : undefined}
      className={({ isActive }) =>
        cn(
          "group/nav relative flex h-9 items-center gap-3 rounded-md text-sm font-medium",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          isActive
            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          collapsed ? "justify-center px-0" : "px-3",
        )
      }
    >
      {({ isActive }) => (
        <>
          {/* 2px brand bar slides in for the active item. */}
          <span
            aria-hidden
            className={cn(
              "absolute left-0 top-1/2 h-4 w-0.5 -translate-y-1/2 rounded-r-full bg-[var(--color-primary)]",
              "transition-opacity duration-[var(--duration-default)]",
              isActive ? "opacity-100" : "opacity-0",
            )}
          />

          <Icon className="h-4 w-4 shrink-0" />

          {!collapsed && (
            <span className="whitespace-nowrap">{item.label}</span>
          )}

          {/* Hover tooltip — only rendered in collapsed mode so it never
              participates in flex sizing of the expanded label slot. */}
          {collapsed && (
            <span
              role="tooltip"
              className={cn(
                "pointer-events-none absolute left-full top-1/2 z-50 ml-3 -translate-y-1/2 whitespace-nowrap",
                "rounded-md border border-[var(--color-border)] bg-[var(--color-popover)] px-2 py-1",
                "text-xs text-[var(--color-popover-foreground)] shadow-[var(--shadow-md)]",
                "opacity-0 transition-opacity duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                "group-hover/nav:opacity-100",
              )}
            >
              {item.label}
            </span>
          )}
        </>
      )}
    </NavLink>
  );
}
