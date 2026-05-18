import { NavLink, Outlet } from "react-router-dom";
import { cn } from "@/lib/cn";

type Tab = { to: string; label: string };

const TABS: Tab[] = [
  { to: "/billing/plans", label: "Plans" },
  { to: "/billing/invoices", label: "Invoices" },
];

/**
 * BillingLayout — page hero + horizontal tabbed sub-nav. Child routes render
 * inside `<Outlet />`. Hero uses the editorial section-rule so Billing slots
 * into the existing admin language even with the new chromatic status chrome
 * landing in the body content below.
 */
export function BillingLayout() {
  return (
    <div className="space-y-6">
      <header className="space-y-3">
        <div className="section-rule">
          <span className="section-rule__crumb">// BILLING</span>
          <span className="section-rule__crumb section-rule__crumb--muted">
            platform pricing &amp; ledger
          </span>
        </div>
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h1 className="font-display text-3xl font-semibold tracking-tight">
              Billing
            </h1>
            <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
              Manage plans, subscriptions, and invoices across every tenant on this instance.
            </p>
          </div>
        </div>
      </header>

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
