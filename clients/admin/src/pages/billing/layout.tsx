import { NavLink, Outlet } from "react-router-dom";
import { CreditCard } from "lucide-react";
import { cn } from "@/lib/cn";
import { EntityPageHeader } from "@/components/list";

type Tab = { to: string; label: string };

const TABS: Tab[] = [
  { to: "/billing/plans", label: "Plans" },
  { to: "/billing/invoices", label: "Invoices" },
];

/**
 * BillingLayout — page hero + horizontal tabbed sub-nav. Child routes render
 * inside `<Outlet />`.
 */
export function BillingLayout() {
  return (
    <div className="space-y-6">
      <EntityPageHeader
        icon={CreditCard}
        tone="saffron"
        title="Billing"
        description="Manage plans, subscriptions, and invoices across every tenant on this instance."
      />

      <nav
        className="flex items-center gap-1 border-b border-[var(--color-border)]"
        aria-label="Billing sections"
      >
        {TABS.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) =>
              cn(
                "relative -mb-px border-b-2 px-4 py-2.5 text-sm font-medium transition-colors",
                isActive
                  ? "border-[var(--color-foreground)] text-[var(--color-foreground)]"
                  : "border-transparent text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
              )
            }
          >
            {tab.label}
          </NavLink>
        ))}
      </nav>

      <div className="pt-1">
        <Outlet />
      </div>
    </div>
  );
}
