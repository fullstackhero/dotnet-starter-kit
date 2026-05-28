import { useCallback, useEffect, useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import {
  ChevronDown,
  PanelLeftClose,
  PanelLeftOpen,
} from "lucide-react";
import { cn } from "@/lib/cn";
import { useAuth } from "@/auth/use-auth";
import {
  findSectionForPath,
  topNavBottom,
  topNavTop,
  visibleItems,
  visibleSections,
  type NavSection,
  type NavSpec,
} from "@/components/layout/nav-data";

const COLLAPSED_KEY = "fsh.sidebar.collapsed";

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
  const location = useLocation();

  // Single-select accordion: which section is currently open. Defaults
  // to the section that owns the current route. Manual clicks override
  // until the user navigates again.
  const [openSection, setOpenSection] = useState<string | null>(() =>
    findSectionForPath(location.pathname),
  );

  // On every route change, re-sync the expanded section to the route's
  // owning section. This re-opens the right section when the user
  // navigates via the command palette, browser back/forward, or any
  // link outside the sidebar.
  useEffect(() => {
    const next = findSectionForPath(location.pathname);
    if (next !== null) {
      setOpenSection(next);
    }
    // If the route is a top-level page (Overview / Settings), close the
    // accordion entirely so no section is highlighted.
    else {
      setOpenSection(null);
    }
  }, [location.pathname]);

  return (
    <aside
      data-collapsed={collapsed || undefined}
      aria-label="Primary navigation"
      className={cn(
        "hidden shrink-0 flex-col border-r border-[var(--color-border)]",
        "bg-[oklch(from_var(--color-card)_l_c_h_/_0.85)] backdrop-blur-xl backdrop-saturate-150 md:flex",
        "transition-[width] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        collapsed ? "w-[52px]" : "w-[220px]",
      )}
    >
      {/* Brand row. */}
      <div
        className={cn(
          "flex h-14 shrink-0 items-center",
          collapsed ? "justify-center px-0" : "justify-between px-4",
        )}
      >
        <div className={cn("flex items-center", collapsed ? "" : "gap-2.5")}>
          <span
            aria-hidden
            className={cn(
              "brand-mark grid size-8 place-items-center rounded-lg shrink-0",
              "font-display text-[12px] font-bold text-[var(--color-primary-foreground)]",
            )}
          >
            F
          </span>
          {!collapsed && (
            <div className="flex flex-col">
              <span className="whitespace-nowrap font-display text-[15px] font-bold leading-none tracking-tight text-[var(--color-foreground)]">
                fullstack<span className="text-[var(--color-primary)]">hero</span>
              </span>
              <span className="mt-1 text-[10px] font-semibold uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
                Dashboard
              </span>
            </div>
          )}
        </div>

        {!collapsed && (
          <button
            type="button"
            onClick={toggle}
            aria-label="Collapse sidebar"
            aria-expanded={!collapsed}
            title="Collapse sidebar"
            className={cn(
              "grid h-7 w-7 shrink-0 cursor-pointer place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <PanelLeftClose className="h-4 w-4" aria-hidden />
          </button>
        )}
      </div>

      <SidebarNavBody
        collapsed={collapsed}
        openSection={openSection}
        setOpenSection={setOpenSection}
      />

      {/* Footer */}
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
              "grid h-7 w-7 shrink-0 cursor-pointer place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <PanelLeftOpen className="h-4 w-4" aria-hidden />
          </button>
        ) : (
          <p className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            v0.1 · dashboard
          </p>
        )}
      </div>
    </aside>
  );
}

// ────────────────────────────────────────────────────────────────────────
// SidebarNavBody — the actual nav (top + section accordions + bottom).
// Extracted so both the desktop <Sidebar> and the mobile drawer can
// render the same nav from one source of truth. Pass `onNavigate` from
// the mobile drawer so item clicks dismiss the sheet.
// ────────────────────────────────────────────────────────────────────────

export function SidebarNavBody({
  collapsed,
  openSection,
  setOpenSection,
  onNavigate,
}: {
  collapsed: boolean;
  openSection: string | null;
  setOpenSection: React.Dispatch<React.SetStateAction<string | null>>;
  /** Called after a nav item link is clicked. Used by the mobile
   *  drawer to close itself on navigation. */
  onNavigate?: () => void;
}) {
  // Hide nav entries the current (or impersonated) user lacks permission for,
  // so they can't navigate to a page the API will reject with 403.
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  const navTop = visibleItems(topNavTop, perms);
  const navSections = visibleSections(perms);
  const navBottom = visibleItems(topNavBottom, perms);

  return (
    /* Nav scrolls vertically when item count exceeds available height.
       `overflow-x: clip` keeps the collapsed-mode hover tooltips from
       spawning a horizontal scrollbar — those tooltips will be
       clipped, but the native title= attribute is the fallback. */
    <nav className="flex-1 space-y-1.5 overflow-y-auto overflow-x-clip px-2.5 py-3.5">
      {/* Top-level: Overview */}
      <div className="space-y-0.5">
        {navTop.map((item) => (
          <NavItemLink key={item.to} item={item} collapsed={collapsed} indent={false} onNavigate={onNavigate} />
        ))}
      </div>

      {/* Section accordions */}
      {!collapsed && (
        <div className="space-y-1.5 pt-1.5">
          {navSections.map((section) => (
            <AccordionSection
              key={section.id}
              section={section}
              isOpen={openSection === section.id}
              onToggle={() =>
                setOpenSection((cur) => (cur === section.id ? null : section.id))
              }
              onNavigate={onNavigate}
            />
          ))}
        </div>
      )}

      {/* Collapsed mode: render every section's items inline as a flat
          list with thin dividers between sections — accordion is a
          label-driven affordance and isn't useful at 64px wide. */}
      {collapsed && (
        <div className="space-y-1 pt-1.5">
          {navSections.map((section, idx) => (
            <div key={section.id}>
              {idx > 0 && (
                <div
                  aria-hidden
                  className="mx-2 my-2 h-px bg-[var(--color-border)]"
                />
              )}
              <div className="space-y-0.5">
                {section.items.map((item) => (
                  <NavItemLink
                    key={item.to}
                    item={item}
                    collapsed
                    indent={false}
                    onNavigate={onNavigate}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Top-level: Settings */}
      <div className="space-y-0.5 pt-1.5">
        {navBottom.map((item) => (
          <NavItemLink key={item.to} item={item} collapsed={collapsed} indent={false} onNavigate={onNavigate} />
        ))}
      </div>
    </nav>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Accordion section (expanded sidebar only).
//
// Closed: a flat hover-able row showing the section icon + caption + a
// small chevron-down on the right.
//
// Open: wrapped in a tone-aware card surface so the active section
// reads as the focal column of the nav. Caption sits up top, items
// below with 2px brand bar on the active item.
// ────────────────────────────────────────────────────────────────────────

function AccordionSection({
  section,
  isOpen,
  onToggle,
  onNavigate,
}: {
  section: NavSection;
  isOpen: boolean;
  onToggle: () => void;
  onNavigate?: () => void;
}) {
  const SectionIcon = section.icon;
  return (
    <div
      className={cn(
        // The "card" treatment for the open state — distinct surface,
        // tone-soft border, gentle inner highlight, comfortable
        // padding. Closed state is borderless and sits flush so the
        // sidebar reads as a list of section labels. Animate every
        // chrome property (bg, border, padding, shadow) so the card
        // materialises rather than snapping in.
        "rounded-lg",
        "transition-[background-color,border-color,box-shadow,padding] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        isOpen
          ? "border border-[var(--color-border)] bg-[var(--color-muted)] p-1.5"
          : "border border-transparent p-0",
      )}
    >
      {/* Section header. Structured to mirror NavItemLink — same height,
          same gap, same padding, same icon size, same text-sm/medium —
          so the sidebar reads as one consistent typographic system.
          Differentiation from a nav item is carried by the trailing
          chevron + the card-surface treatment when this section is
          active, not by font/size shifts. */}
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={isOpen}
        aria-controls={`nav-section-${section.id}`}
        className={cn(
          "flex h-9 w-full cursor-pointer items-center gap-3 rounded-md px-3",
          "text-left text-sm font-medium",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          isOpen
            ? "text-[var(--color-foreground)]"
            : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        )}
      >
        <SectionIcon className="h-4 w-4 shrink-0" aria-hidden />
        <span className="flex-1 truncate">{section.caption}</span>
        <ChevronDown
          aria-hidden
          className={cn(
            "h-3.5 w-3.5 shrink-0 transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
            isOpen ? "rotate-180 text-[var(--color-foreground)]" : "rotate-0 text-[var(--color-muted-foreground)]",
          )}
        />
      </button>

      {/* Items panel — animated open/close via the grid-template-rows
          0fr ↔ 1fr trick. The wrapper is a CSS grid with a single
          implicit row whose track size animates between 0fr (closed,
          panel collapses) and 1fr (open, panel takes its natural
          height). The inner div needs `overflow: hidden` + `min-h-0`
          so the contents are clipped during the transition rather
          than overflowing into the next section. Items also fade in
          with a tiny delay so the slide and the visual reveal stay
          in lockstep. */}
      <div
        id={`nav-section-${section.id}`}
        className={cn(
          "grid",
          "transition-[grid-template-rows,margin-top] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
          isOpen ? "mt-1 grid-rows-[1fr]" : "mt-0 grid-rows-[0fr]",
        )}
      >
        <div className="min-h-0 overflow-hidden">
          <div
            aria-hidden={!isOpen}
            className={cn(
              "space-y-0.5",
              "transition-opacity ease-[var(--ease-out-cubic)]",
              isOpen
                ? "opacity-100 duration-[var(--duration-default)] delay-[80ms]"
                : "opacity-0 duration-[var(--duration-fast)]",
            )}
          >
            {section.items.map((item) => (
              <NavItemLink key={item.to} item={item} collapsed={false} indent onNavigate={onNavigate} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Nav item link. Reused for top-level items (Overview, Settings),
// inside an open accordion (indent=true), and inside the collapsed
// sidebar's flat icon stack.
// ────────────────────────────────────────────────────────────────────────

function NavItemLink({
  item,
  collapsed,
  indent,
  onNavigate,
}: {
  item: NavSpec;
  collapsed: boolean;
  /** Adds a small left padding so accordion items align under the
   *  section caption with breathing room. Not applied to top-level. */
  indent: boolean;
  /** Fired after the link click. Used by the mobile sheet to close
   *  itself once the user navigates somewhere. */
  onNavigate?: () => void;
}) {
  const Icon = item.icon;
  return (
    <NavLink
      to={item.to}
      end={item.to === "/"}
      title={collapsed ? item.label : undefined}
      // When collapsed the text label is hidden, so the icon-only link needs
      // an explicit accessible name (title alone is the weakest AT signal).
      aria-label={collapsed ? item.label : undefined}
      onClick={onNavigate}
      className={({ isActive }) =>
        cn(
          "group/nav relative flex h-9 items-center gap-3 rounded-md text-sm font-medium",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          isActive
            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          collapsed ? "justify-center px-0" : indent ? "px-3" : "px-3",
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

          {/* Tooltip in collapsed mode — surfaces on hover OR keyboard
              focus so keyboard-only users can discover the label. The
              `title=` attribute on the link is the fallback for AT users
              who never see the popup. */}
          {collapsed && (
            <span
              role="tooltip"
              className={cn(
                "pointer-events-none absolute left-full top-1/2 z-50 ml-3 -translate-y-1/2 whitespace-nowrap",
                "rounded-md border border-[var(--color-border)] bg-[var(--color-popover)] px-2 py-1",
                "text-xs text-[var(--color-popover-foreground)] shadow-[var(--shadow-md)]",
                "opacity-0 transition-opacity duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                "group-hover/nav:opacity-100 group-focus-visible/nav:opacity-100",
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
