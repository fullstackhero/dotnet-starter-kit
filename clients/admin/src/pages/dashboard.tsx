import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowRight,
  Building2,
  FileText,
  LayoutDashboard,
  Receipt,
  UsersRound,
} from "lucide-react";
import { listTenants } from "@/api/tenants";
import { listInvoices, getPlans } from "@/api/billing";
import { Skeleton } from "@/components/ui/skeleton";
import { EntityPageHeader, Stat, StatStrip, ToneIconTile, type ToneIconTileTone } from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { formatNumber } from "@/lib/format";
import { cn } from "@/lib/cn";

/**
 * DashboardPage — the operator overview. EntityPageHeader greeting,
 * four KPI stat tiles drawing from real data, then pivot cards into
 * the rest of the app. No fake "Coming soon" filler.
 */
export function DashboardPage() {
  const { t } = useTranslation("dashboard");
  const { user } = useAuth();

  const tenantsQuery = useQuery({
    queryKey: ["tenants", { pageNumber: 1, pageSize: 1 }],
    queryFn: () => listTenants({ pageNumber: 1, pageSize: 1 }),
  });
  const plansQuery = useQuery({
    queryKey: ["billing", "plans", { includeInactive: true }],
    queryFn: () => getPlans(true),
  });
  const invoicesQuery = useQuery({
    queryKey: ["billing", "invoices", { pageNumber: 1, pageSize: 50 }],
    queryFn: () => listInvoices({ pageNumber: 1, pageSize: 50 }),
  });

  const tenantsTotal = tenantsQuery.data?.totalCount;
  const plans = plansQuery.data ?? [];
  const activePlans = plans.filter((p) => p.isActive).length;
  const invoicesPage = invoicesQuery.data;
  const outstandingCount =
    invoicesPage?.items.filter((i) => i.status === "Issued").length ?? 0;

  const firstName = user?.name?.split(" ")[0];

  return (
    <div className="space-y-6">
      {/* ── Page header ──────────────────────────────────────────────── */}
      <div className="fsh-enter">
        <EntityPageHeader
          icon={LayoutDashboard}
          title={
            <>
              {t("title")}{firstName ? (
                <span className="text-[var(--color-muted-foreground)]">{t("greeting", { name: firstName })}</span>
              ) : null}
            </>
          }
          tone="primary"
          description={t("description")}
        />
      </div>

      {/* ── KPI stat strip ───────────────────────────────────────────── */}
      <StatStrip cols={4} className="fsh-enter fsh-enter-2">
        <Stat
          label={t("stat.tenants")}
          value={
            tenantsQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              tenantsTotal != null ? formatNumber(tenantsTotal) : "—"
            )
          }
          hint={t("stat.tenantsHint")}
        />
        <Stat
          label={t("stat.plans")}
          value={
            plansQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              formatNumber(plans.length)
            )
          }
          hint={t("stat.plansActive", { count: activePlans })}
        />
        <Stat
          label={t("stat.invoices")}
          value={
            invoicesQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              invoicesPage != null ? formatNumber(invoicesPage.items.length) : "—"
            )
          }
          hint={
            invoicesPage
              ? t("stat.invoicesHint", { count: invoicesPage.totalCount })
              : t("stat.invoicesLoading")
          }
        />
        <Stat
          label={t("stat.outstanding")}
          value={
            invoicesQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              formatNumber(outstandingCount)
            )
          }
          hint={t("stat.outstandingHint")}
          tone={outstandingCount > 0 ? "warning" : "default"}
        />
      </StatStrip>

      {/* ── Quick pivots ─────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-3 space-y-3">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          {t("entryPoints")}
        </p>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <PivotCard
            to="/tenants"
            icon={Building2}
            tone="info"
            title={t("pivot.tenants.title")}
            description={t("pivot.tenants.description")}
          />
          <PivotCard
            to="/users"
            icon={UsersRound}
            tone="primary"
            title={t("pivot.users.title")}
            description={t("pivot.users.description")}
          />
          <PivotCard
            to="/billing/plans"
            icon={Receipt}
            tone="success"
            title={t("pivot.billing.title")}
            description={t("pivot.billing.description")}
          />
          <PivotCard
            to="/billing/invoices"
            icon={FileText}
            tone="warning"
            title={t("pivot.invoices.title")}
            description={t("pivot.invoices.description")}
          />
        </div>
      </section>
    </div>
  );
}

// ─── subcomponents ───────────────────────────────────────────────────

function PivotCard({
  to,
  icon: Icon,
  tone,
  title,
  description,
}: {
  to: string;
  icon: typeof Building2;
  tone: ToneIconTileTone;
  title: string;
  description: string;
}) {
  return (
    <Link to={to} className="group block focus:outline-none">
      <div
        className={cn(
          "flex h-full flex-col gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs",
          "transition-colors duration-200 hover:border-[var(--color-border-strong)] hover:bg-[var(--color-accent)]",
        )}
      >
        <div className="flex items-start justify-between">
          <ToneIconTile icon={Icon} tone={tone} size="md" />
          <ArrowRight
            aria-hidden
            className="size-3.5 text-[var(--color-muted-foreground)] opacity-0 transition-all duration-200 group-hover:translate-x-0.5 group-hover:opacity-100"
          />
        </div>
        <div>
          <div className="font-display text-[14px] font-semibold tracking-tight text-[var(--color-foreground)]">
            {title}
          </div>
          <p className="mt-0.5 text-[12px] leading-snug text-[var(--color-muted-foreground)]">
            {description}
          </p>
        </div>
      </div>
    </Link>
  );
}
