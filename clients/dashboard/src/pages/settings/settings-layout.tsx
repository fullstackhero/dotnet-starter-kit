import { NavLink, Outlet } from "react-router-dom";
import {
  Bell,
  KeyRound,
  Palette,
  Shield,
  UserRound,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { PageHero } from "@/components/list";
import { cn } from "@/lib/cn";

type Tab = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
};

const tabs: Tab[] = [
  { to: "/settings/profile", label: "Profile", icon: UserRound },
  { to: "/settings/security", label: "Security", icon: Shield },
  { to: "/settings/appearance", label: "Appearance", icon: Palette },
  { to: "/settings/notifications", label: "Notifications", icon: Bell },
  { to: "/settings/api-keys", label: "API keys", icon: KeyRound },
];

/**
 * Settings page chrome — header, pill tab nav, and an Outlet for the
 * active tab content. Each tab is a deep-linkable nested route, so
 * /settings/security is bookmarkable.
 */
export function SettingsLayout() {
  const { user } = useAuth();
  return (
    <div className="space-y-7">
      <PageHero
        eyebrow="Account · Settings"
        tenant={user?.tenant ?? "—"}
        title="Settings"
        subtitle="Manage your profile, security, appearance, and tenant preferences."
      />

      {/* Pill tab nav. flex-wrap so it lays sensibly on narrow viewports;
          the active pill takes brand-soft + brand text, idle pills sit
          on the surface with a hover hint. The `-mx-1` cancels the
          pill's own padding so the active background lines up flush
          with whatever surface sits beneath. */}
      <nav
        aria-label="Settings sections"
        className="fsh-enter fsh-enter-2 -mx-1 flex flex-wrap gap-1"
      >
        {tabs.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end
            className={({ isActive }) =>
              cn(
                "group/pill inline-flex h-9 items-center gap-1.5 rounded-full px-3.5 text-sm font-medium",
                "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--color-background)]",
                isActive
                  ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              )
            }
          >
            <Icon className="h-3.5 w-3.5" aria-hidden />
            {label}
          </NavLink>
        ))}
      </nav>

      {/* Active tab content — each tab module applies its own
          fsh-enter classes so swaps replay the entrance stagger. */}
      <section>
        <Outlet />
      </section>
    </div>
  );
}
