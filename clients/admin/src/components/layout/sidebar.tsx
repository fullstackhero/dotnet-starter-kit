import { useCallback, useEffect, useMemo, useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ChevronDown, PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { cn } from "@/lib/cn";
import { useAuth } from "@/auth/use-auth";
import {
  findSectionForPath,
  filterNavSpec,
  sections,
  topNavBottom,
  topNavTop,
  type NavSection,
  type NavSpec,
} from "@/components/layout/nav-items";

const COLLAPSED_KEY = "fsh.admin.sidebar.collapsed";

/** Persisted collapsed state, reads localStorage on mount. */
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
  return { collapsed, toggle: () => setCollapsed(!collapsed) };
}

export function Sidebar() {
  const { collapsed, toggle } = useCollapsedSidebar();
  const location = useLocation();
  const { user, permissionsHydrated } = useAuth();
  const { t } = useTranslation("common");

  // Permission-filtered sections
  const granted = permissionsHydrated ? (user?.permissions ?? []) : [];
  const visibleSections: NavSection[] = useMemo(
    () =>
      sections
        .map((s) => ({ ...s, items: filterNavSpec(s.items, granted) }))
        .filter((s) => s.items.length > 0),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [granted.join(",")],
  );

  // Single-select accordion: which section is currently open.
  const [openSection, setOpenSection] = useState<string | null>(() =>
    findSectionForPath(location.pathname),
  );

  // Re-sync the open section on every route change.
  useEffect(() => {
    const next = findSectionForPath(location.pathname);
    setOpenSection(next);
  }, [location.pathname]);

  return (
    <aside
      data-collapsed={collapsed || undefined}
      aria-label={t("shell.primaryNav")}
      className={cn(
        "hidden shrink-0 flex-col border-r border-[var(--color-border)]",
        "bg-[oklch(from_var(--color-card)_l_c_h_/_0.85)] backdrop-blur-xl backdrop-saturate-150 md:flex",
        "transition-[width] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        collapsed ? "w-[52px]" : "w-[220px]",
      )}
    >
      {/* Brand row */}
      <div
        className={cn(
          "flex h-14 shrink-0 items-center border-b border-[var(--color-border)]",
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
                {t("shell.appSubtitle")}
              </span>
            </div>
          )}
        </div>

        {!collapsed && (
          <button
            type="button"
            onClick={toggle}
            aria-label={t("shell.collapseSidebar")}
            aria-expanded={!collapsed}
            title={t("shell.collapseSidebar")}
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
        visibleSections={visibleSections}
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
            aria-label={t("shell.expandSidebar")}
            aria-expanded={false}
            title={t("shell.expandSidebar")}
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
            v0.1 · admin
          </p>
        )}
      </div>
    </aside>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// SidebarNavBody — the nav (top singletons + section accordions + bottom).
// Extracted so the desktop Sidebar and the mobile drawer share one source.
// ─────────────────────────────────────────────────────────────────────────────

export function SidebarNavBody({
  collapsed,
  openSection,
  setOpenSection,
  visibleSections,
  onNavigate,
}: {
  collapsed: boolean;
  openSection: string | null;
  setOpenSection: React.Dispatch<React.SetStateAction<string | null>>;
  visibleSections: NavSection[];
  /** Called after a nav item click — used by the mobile sheet to close itself. */
  onNavigate?: () => void;
}) {
  const { t } = useTranslation("common");
  return (
    <nav
      className="flex-1 space-y-1.5 overflow-y-auto overflow-x-clip px-2.5 py-3.5"
      aria-label={t("shell.primaryNav")}
    >
      {/* Top-level singletons: Overview */}
      <div className="space-y-0.5">
        {topNavTop.map((item) => (
          <NavItemLink
            key={item.to}
            item={item}
            collapsed={collapsed}
            indent={false}
            onNavigate={onNavigate}
          />
        ))}
      </div>

      {/* Section accordions (expanded mode) */}
      {!collapsed && (
        <div className="space-y-1.5 pt-1.5">
          {visibleSections.map((section) => (
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

      {/* Collapsed mode — flat icon stack with thin dividers between sections. */}
      {collapsed && (
        <div className="space-y-1 pt-1.5">
          {visibleSections.map((section, idx) => (
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

      {/* Bottom singletons: Settings */}
      <div className="space-y-0.5 pt-1.5">
        {topNavBottom.map((item) => (
          <NavItemLink
            key={item.to}
            item={item}
            collapsed={collapsed}
            indent={false}
            onNavigate={onNavigate}
          />
        ))}
      </div>
    </nav>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// AccordionSection
// ─────────────────────────────────────────────────────────────────────────────

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
  const { t } = useTranslation("nav");
  const SectionIcon = section.icon;
  return (
    <div
      className={cn(
        "rounded-lg",
        "transition-[background-color,border-color,box-shadow,padding] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        isOpen
          ? "border border-[var(--color-border)] bg-[var(--color-muted)] p-1.5"
          : "border border-transparent p-0",
      )}
    >
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
        <span className="flex-1 truncate">{t(section.caption)}</span>
        <ChevronDown
          aria-hidden
          className={cn(
            "h-3.5 w-3.5 shrink-0 transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
            isOpen
              ? "rotate-180 text-[var(--color-foreground)]"
              : "rotate-0 text-[var(--color-muted-foreground)]",
          )}
        />
      </button>

      {/* Animated open/close via grid-template-rows trick */}
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
              <NavItemLink
                key={item.to}
                item={item}
                collapsed={false}
                indent
                onNavigate={onNavigate}
              />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// NavItemLink — used for top-level singletons, accordion children, and
// collapsed-mode icon stacks.
// ─────────────────────────────────────────────────────────────────────────────

function NavItemLink({
  item,
  collapsed,
  onNavigate,
}: {
  item: NavSpec;
  collapsed: boolean;
  /** Reserved for layout variants — not yet used but kept in the call-site API. */
  indent?: boolean;
  onNavigate?: () => void;
}) {
  const { t } = useTranslation("nav");
  const Icon = item.icon;
  const label = t(item.label);
  return (
    <NavLink
      to={item.to}
      end={item.to === "/"}
      title={collapsed ? label : undefined}
      aria-label={collapsed ? label : undefined}
      onClick={onNavigate}
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
          {/* 2px brand bar for active item */}
          <span
            aria-hidden
            className={cn(
              "absolute left-0 top-1/2 h-4 w-0.5 -translate-y-1/2 rounded-r-full bg-[var(--color-primary)]",
              "transition-opacity duration-[var(--duration-default)]",
              isActive ? "opacity-100" : "opacity-0",
            )}
          />

          <Icon className="h-4 w-4 shrink-0" aria-hidden />

          {!collapsed && <span className="whitespace-nowrap">{label}</span>}

          {/* Tooltip in collapsed mode */}
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
              {label}
            </span>
          )}
        </>
      )}
    </NavLink>
  );
}
