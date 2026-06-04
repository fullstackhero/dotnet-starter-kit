import { Link } from "react-router-dom";
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
import { cn } from "@/lib/cn";

/**
 * DashboardPage — the operator overview. EntityPageHeader greeting,
 * four KPI stat tiles drawing from real data, then pivot cards into
 * the rest of the app. No fake "Coming soon" filler.
 */
export function DashboardPage() {
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
              Overview{firstName ? (
                <span className="text-[var(--color-muted-foreground)]">, {firstName}</span>
              ) : null}
            </>
          }
          tone="primary"
          description="Operate every tenant on this instance — identity, multitenancy, billing, and the rest of the system surface."
        />
      </div>

      {/* ── KPI stat strip ───────────────────────────────────────────── */}
      <StatStrip cols={4} className="fsh-enter fsh-enter-2">
        <Stat
          label="Tenants"
          value={
            tenantsQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              tenantsTotal?.toLocaleString() ?? "—"
            )
          }
          hint="registered on this instance"
        />
        <Stat
          label="Plans"
          value={
            plansQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              plans.length.toLocaleString()
            )
          }
          hint={`${activePlans} active`}
        />
        <Stat
          label="Invoices"
          value={
            invoicesQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              invoicesPage?.items.length.toLocaleString() ?? "—"
            )
          }
          hint={
            invoicesPage
              ? `${invoicesPage.totalCount.toLocaleString()} total ledger`
              : "loading…"
          }
        />
        <Stat
          label="Outstanding"
          value={
            invoicesQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              outstandingCount.toLocaleString()
            )
          }
          hint="issued, awaiting payment"
          tone={outstandingCount > 0 ? "warning" : "default"}
        />
      </StatStrip>

      {/* ── Quick pivots ─────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-3 space-y-3">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          Entry points
        </p>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <PivotCard
            to="/tenants"
            icon={Building2}
            tone="info"
            title="Tenants"
            description="Provision, suspend, and inspect tenants."
          />
          <PivotCard
            to="/users"
            icon={UsersRound}
            tone="primary"
            title="Users"
            description="Root-tenant operators and role management."
          />
          <PivotCard
            to="/billing/plans"
            icon={Receipt}
            tone="success"
            title="Billing"
            description="Plans, subscriptions, invoices and pricing."
          />
          <PivotCard
            to="/billing/invoices"
            icon={FileText}
            tone="warning"
            title="Invoices"
            description="Cross-tenant ledger. Issue, mark paid, void."
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
