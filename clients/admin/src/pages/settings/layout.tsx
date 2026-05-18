import { NavLink, Outlet } from "react-router-dom";
import { MonitorSmartphone, Palette, ShieldCheck, UserRound } from "lucide-react";
import { PageHeader } from "@/components/list";
import { cn } from "@/lib/cn";

type Tab = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
};

const TABS: Tab[] = [
  { to: "/settings/profile", label: "Profile", icon: UserRound },
  { to: "/settings/security", label: "Security", icon: ShieldCheck },
  { to: "/settings/sessions", label: "Sessions", icon: MonitorSmartphone },
  { to: "/settings/appearance", label: "Appearance", icon: Palette },
];

/**
 * SettingsLayout — header + pill tab nav + outlet for the active tab.
 * Mirrors dashboard's settings shell but in Console aesthetic: mono-caps
 * labels in the active pill, hairline borders, no brand-soft fills.
 */
export function SettingsLayout() {
  return (
    <div className="space-y-7">
      <PageHeader
        crumbs={[{ label: "\\ Settings" }, { label: "Account", muted: true }]}
        title="Settings"
        description="Manage your profile, credentials, sessions, and appearance preferences."
      />

      <nav
        aria-label="Settings sections"
        className="-mx-1 flex flex-wrap items-center gap-1 border-b border-[var(--color-border)] pb-0 fsh-enter fsh-enter-2"
      >
        {TABS.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end
            className={({ isActive }) =>
              cn(
                "relative inline-flex h-10 items-center gap-1.5 px-3.5 text-sm font-medium",
                "transition-colors duration-[var(--duration-fast)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                isActive
                  ? "text-[var(--color-foreground)] after:absolute after:inset-x-2 after:-bottom-px after:h-[2px] after:bg-[var(--color-accent-signal)]"
                  : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
              )
            }
          >
            <Icon className="h-3.5 w-3.5" aria-hidden />
            {label}
          </NavLink>
        ))}
      </nav>

      <section>
        <Outlet />
      </section>
    </div>
  );
}
