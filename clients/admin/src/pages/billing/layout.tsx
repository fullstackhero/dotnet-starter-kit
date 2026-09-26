import { NavLink, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { CreditCard } from "lucide-react";
import { cn } from "@/lib/cn";
import { EntityPageHeader } from "@/components/list";

type Tab = { to: string; labelKey: string };

const TABS: Tab[] = [
  { to: "/billing/plans", labelKey: "layout.tab.plans" },
  { to: "/billing/invoices", labelKey: "layout.tab.invoices" },
  { to: "/billing/topups", labelKey: "layout.tab.topups" },
];

/**
 * BillingLayout — page hero + horizontal tabbed sub-nav. Child routes render
 * inside `<Outlet />`.
 */
export function BillingLayout() {
  const { t } = useTranslation("billing");
  return (
    <div className="space-y-6">
      <EntityPageHeader
        icon={CreditCard}
        tone="saffron"
        title={t("layout.title")}
        description={t("layout.description")}
      />

      <nav
        className="flex items-center gap-1 border-b border-[var(--color-border)]"
        aria-label={t("layout.sectionsAria")}
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
            {t(tab.labelKey)}
          </NavLink>
        ))}
      </nav>

      <div className="pt-1">
        <Outlet />
      </div>
    </div>
  );
}
