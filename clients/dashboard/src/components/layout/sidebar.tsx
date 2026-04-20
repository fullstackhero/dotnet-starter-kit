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
    <aside className="hidden w-60 shrink-0 border-r border-[var(--color-border)] bg-[var(--color-card)] md:flex md:flex-col">
      <div className="flex h-14 items-center gap-2 border-b border-[var(--color-border)] px-5">
        <div className="h-6 w-6 rounded bg-[var(--color-primary)]" aria-hidden />
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
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-[var(--color-accent)] text-[var(--color-accent-foreground)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-accent-foreground)]",
              )
            }
          >
            <Icon className="h-4 w-4" />
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
