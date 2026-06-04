import { NavLink, Outlet, useLocation } from "react-router-dom";
import {
  Bell,
  ChevronRight,
  KeyRound,
  Palette,
  Settings as SettingsIcon,
  Shield,
  UserRound,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { EntityPageHeader } from "@/components/list";
import { cn } from "@/lib/cn";

type Tab = {
  to: string;
  label: string;
  hint: string;
  icon: LucideIcon;
};

const TABS: Tab[] = [
  { to: "/settings/profile", label: "Profile", hint: "Your identity across the tenant", icon: UserRound },
  { to: "/settings/security", label: "Security", hint: "Password and active sessions", icon: Shield },
  { to: "/settings/appearance", label: "Appearance", hint: "Theme and visual preferences", icon: Palette },
  { to: "/settings/notifications", label: "Notifications", hint: "How we reach you", icon: Bell },
  { to: "/settings/api-keys", label: "API keys", hint: "Personal access tokens", icon: KeyRound },
];

const pad2 = (n: number) => n.toString().padStart(2, "0");

/**
 * Settings shell — editorial left nav + content. Each tab is a deep-linkable
 * nested route, so /settings/security stays bookmarkable. Visually mirrors
 * the dentalOS settings page: numbered vertical nav on the left, "Section 0X"
 * masthead at the top of the content, sections rendered as warm-paper cards.
 */
export function SettingsLayout() {
  const location = useLocation();
  const activeIndex = Math.max(
    0,
    TABS.findIndex((t) => location.pathname.startsWith(t.to)),
  );
  const active = TABS[activeIndex] ?? TABS[0];

  return (
    <div className="space-y-6">
      {/* Page header — title resolves to "Settings · {active section}" so the
          masthead lives inline with the page title instead of stacking a
          second header inside the right column. */}
      <EntityPageHeader
        icon={SettingsIcon}
        title={
          <span className="flex flex-wrap items-baseline gap-x-2.5 gap-y-1">
            <span>Settings</span>
            <span
              aria-hidden
              className="text-[oklch(from_var(--color-border-strong)_l_c_h_/_0.7)]"
            >
              ·
            </span>
            <span className="font-display text-[20px] font-semibold tracking-tight text-[var(--color-foreground)]">
              {active.label}
            </span>
          </span>
        }
        description={active.hint}
      />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[260px_1fr] lg:gap-10">
        {/* ─── Editorial left nav ─── */}
        <nav aria-label="Settings sections">
          {/* Desktop: vertical numbered list */}
          <div className="sticky top-6 hidden lg:block">
            <p className="mb-4 pl-5 text-[10px] font-semibold uppercase tracking-[0.18em] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
              Sections
            </p>
            <ul className="relative space-y-px">
              {/* Faint vertical rail tying the numbers together */}
              <div
                aria-hidden
                className="absolute left-[14px] top-1 bottom-1 w-px bg-[oklch(from_var(--color-border)_l_c_h_/_0.6)]"
              />
              {TABS.map((t, i) => {
                const num = pad2(i + 1);
                return (
                  <li key={t.to}>
                    <NavLink
                      to={t.to}
                      end
                      className={({ isActive }) =>
                        cn(
                          "group relative flex w-full items-start gap-3 rounded-lg pl-5 pr-3 py-3 text-left transition-all cursor-pointer",
                          isActive
                            ? "bg-[var(--color-card)] shadow-xs"
                            : "hover:bg-[oklch(from_var(--color-muted)_l_c_h_/_0.4)]",
                        )
                      }
                    >
                      {({ isActive }) => (
                        <>
                          {isActive && (
                            <span
                              aria-hidden
                              className="absolute left-0 top-1/2 h-7 w-[3px] -translate-y-1/2 rounded-full bg-[var(--color-primary)]"
                            />
                          )}
                          <span
                            className={cn(
                              "z-10 mt-0.5 bg-[var(--color-background)] px-1 font-display text-[11px] font-semibold leading-5 tabular-nums transition-colors",
                              isActive
                                ? "text-[var(--color-primary)]"
                                : "text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]",
                            )}
                          >
                            {num}
                          </span>
                          <span className="min-w-0 flex-1">
                            <span
                              className={cn(
                                "block text-[13px] font-semibold transition-colors",
                                isActive
                                  ? "text-[var(--color-foreground)]"
                                  : "text-[var(--color-muted-foreground)] group-hover:text-[var(--color-foreground)]",
                              )}
                            >
                              {t.label}
                            </span>
                            <span className="mt-0.5 block truncate text-[11px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
                              {t.hint}
                            </span>
                          </span>
                          <ChevronRight
                            aria-hidden
                            className={cn(
                              "mt-1 size-3.5 shrink-0 transition-all",
                              isActive
                                ? "translate-x-0.5 text-[var(--color-primary)]"
                                : "text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.3)] group-hover:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]",
                            )}
                          />
                        </>
                      )}
                    </NavLink>
                  </li>
                );
              })}
            </ul>
          </div>

          {/* Mobile: horizontal scroll tabs */}
          <div className="-mx-2 overflow-x-auto pb-1 lg:hidden">
            <div className="flex gap-1 px-2">
              {TABS.map(({ to, label, icon: Icon }) => (
                <NavLink
                  key={to}
                  to={to}
                  end
                  className={({ isActive }) =>
                    cn(
                      "inline-flex h-9 shrink-0 items-center gap-1.5 rounded-full px-3.5 text-[12px] font-medium",
                      "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                      isActive
                        ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                        : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                    )
                  }
                >
                  <Icon className="size-3.5" aria-hidden />
                  {label}
                </NavLink>
              ))}
            </div>
          </div>
        </nav>

        {/* ─── Tab content ─── */}
        <div className="min-w-0">
          <div className="space-y-5">
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  SettingsSection — warm-paper section card with optional header bar
//  and footer bar. Drop-in for the per-tab section groupings.
// ───────────────────────────────────────────────────────────────────────

export function SettingsSection({
  title,
  icon: Icon,
  description,
  footer,
  className,
  children,
}: {
  title?: string;
  icon?: LucideIcon;
  description?: React.ReactNode;
  footer?: React.ReactNode;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <section
      className={cn(
        "overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
        "shadow-xs",
        className,
      )}
    >
      {title && (
        <div className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-5 py-3">
          <h2 className="flex items-center gap-2 text-[13px] font-semibold text-[var(--color-foreground)]">
            {Icon && (
              <Icon className="size-3.5 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
            )}
            {title}
          </h2>
          {description && (
            <p className="mt-1 text-[12px] text-[var(--color-muted-foreground)]">
              {description}
            </p>
          )}
        </div>
      )}
      <div className="px-5 py-5">{children}</div>
      {footer && (
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-5 py-3">
          {footer}
        </div>
      )}
    </section>
  );
}
