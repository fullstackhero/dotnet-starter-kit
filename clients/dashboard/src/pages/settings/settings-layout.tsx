import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  Bell,
  Brush,
  ChevronRight,
  KeyRound,
  Palette,
  Settings as SettingsIcon,
  Shield,
  UserRound,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { EntityPageHeader } from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";

type Tab = {
  to: string;
  labelKey: string;
  hintKey: string;
  icon: LucideIcon;
  /**
   * Permission required to see this tab. Tabs without a `perm` are visible to
   * every authenticated tenant user; gated tabs hide when the current user
   * lacks the permission, mirroring the sidebar gate in nav-data.ts so they
   * never land on a page the API would reject with 403.
   */
  perm?: string;
};

const TABS: Tab[] = [
  { to: "/settings/profile", labelKey: "tab.profile.label", hintKey: "tab.profile.hint", icon: UserRound },
  { to: "/settings/security", labelKey: "tab.security.label", hintKey: "tab.security.hint", icon: Shield },
  { to: "/settings/appearance", labelKey: "tab.appearance.label", hintKey: "tab.appearance.hint", icon: Palette },
  // Tenant-wide branding (palette + logos served on sign-in), distinct from the
  // per-user Appearance prefs above. Gated on the same permission the /theme
  // endpoints enforce server-side.
  { to: "/settings/branding", labelKey: "tab.branding.label", hintKey: "tab.branding.hint", icon: Brush, perm: "Permissions.Tenants.UpdateTheme" },
  { to: "/settings/notifications", labelKey: "tab.notifications.label", hintKey: "tab.notifications.hint", icon: Bell },
  { to: "/settings/api-keys", labelKey: "tab.apiKeys.label", hintKey: "tab.apiKeys.hint", icon: KeyRound },
];

const pad2 = (n: number) => n.toString().padStart(2, "0");

/**
 * Settings shell — editorial left nav + content. Each tab is a deep-linkable
 * nested route, so /settings/security stays bookmarkable. Visually mirrors
 * the dentalOS settings page: numbered vertical nav on the left, "Section 0X"
 * masthead at the top of the content, sections rendered as warm-paper cards.
 */
export function SettingsLayout() {
  const { t } = useTranslation("settings");
  const location = useLocation();
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  // Drop tabs the user can't reach, same gate the sidebar uses. `branding`
  // hides for users without Tenants.UpdateTheme.
  const tabs = TABS.filter((tab) => !tab.perm || perms.includes(tab.perm));
  const activeIndex = Math.max(
    0,
    tabs.findIndex((tab) => location.pathname.startsWith(tab.to)),
  );
  const active = tabs[activeIndex] ?? tabs[0];

  return (
    <div className="space-y-6">
      {/* Page header — title resolves to "Settings · {active section}" so the
          masthead lives inline with the page title instead of stacking a
          second header inside the right column. */}
      <EntityPageHeader
        icon={SettingsIcon}
        title={
          <span className="flex flex-wrap items-baseline gap-x-2.5 gap-y-1">
            <span>{t("title")}</span>
            <span
              aria-hidden
              className="text-[oklch(from_var(--color-border-strong)_l_c_h_/_0.7)]"
            >
              ·
            </span>
            <span className="font-display text-[20px] font-semibold tracking-tight text-[var(--color-foreground)]">
              {t(active.labelKey)}
            </span>
          </span>
        }
        description={t(active.hintKey)}
      />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[260px_1fr] lg:gap-10">
        {/* ─── Editorial left nav ─── */}
        <nav aria-label={t("sectionsNav")}>
          {/* Desktop: vertical numbered list */}
          <div className="sticky top-6 hidden lg:block">
            <p className="mb-4 pl-5 text-[10px] font-semibold uppercase tracking-[0.18em] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
              {t("sections")}
            </p>
            <ul className="relative space-y-px">
              {/* Faint vertical rail tying the numbers together */}
              <div
                aria-hidden
                className="absolute left-[14px] top-1 bottom-1 w-px bg-[oklch(from_var(--color-border)_l_c_h_/_0.6)]"
              />
              {tabs.map((tab, i) => {
                const num = pad2(i + 1);
                return (
                  <li key={tab.to}>
                    <NavLink
                      to={tab.to}
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
                              {t(tab.labelKey)}
                            </span>
                            <span className="mt-0.5 block truncate text-[11px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
                              {t(tab.hintKey)}
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
              {tabs.map(({ to, labelKey, icon: Icon }) => (
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
                  {t(labelKey)}
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
