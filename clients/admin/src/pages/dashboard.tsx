import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowUpRight, Building2, FileText, Receipt, UsersRound } from "lucide-react";
import { listTenants } from "@/api/tenants";
import { listInvoices, getPlans } from "@/api/billing";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { KpiTile } from "@/components/kpi-tile";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/auth/use-auth";

/**
 * DashboardPage — the Console "overview." A live system status header, four
 * KPI tiles drawing from real data, then quick-pivot cards into the rest of
 * the app. No fake "Coming soon" filler; every panel is either real data
 * or removed.
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

  return (
    <div className="space-y-8">
      {/* ── Hero header ──────────────────────────────────────────────── */}
      <header className="fsh-enter space-y-3">
        <div className="section-rule">
          <span className="section-rule__crumb">// OVERVIEW</span>
          <span className="section-rule__crumb section-rule__crumb--muted">
            platform status
          </span>
          <span className="ml-auto inline-flex items-center gap-2">
            <span className="pulse-dot" aria-hidden />
            <span className="meta text-[var(--color-muted-foreground)]">live</span>
          </span>
        </div>
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h1 className="font-display text-4xl font-semibold leading-none tracking-tight">
              Console{user?.name ? <span className="text-[var(--color-muted-foreground)]">, {user.name.split(" ")[0]}</span> : null}
              <span className="text-[var(--color-accent-signal)]">.</span>
            </h1>
            <p className="mt-2 max-w-xl text-sm text-[var(--color-muted-foreground)] leading-relaxed">
              Operate every tenant on this instance — identity, multitenancy, billing,
              and the rest of the system surface, from one place.
            </p>
          </div>
        </div>
      </header>

      {/* ── KPI strip ────────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-2 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <KpiTile
          label="Tenants"
          value={
            tenantsQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              tenantsTotal?.toLocaleString() ?? "—"
            )
          }
          subtitle="registered on this instance"
        />
        <KpiTile
          label="Plans"
          value={
            plansQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              plans.length.toLocaleString()
            )
          }
          subtitle={`${activePlans} active`}
        />
        <KpiTile
          label="Invoices · this page"
          value={
            invoicesQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              invoicesPage?.items.length.toLocaleString() ?? "—"
            )
          }
          subtitle={
            invoicesPage
              ? `${invoicesPage.totalCount.toLocaleString()} total ledger`
              : "loading…"
          }
        />
        <KpiTile
          label="Outstanding"
          value={
            invoicesQuery.isLoading ? (
              <Skeleton className="h-7 w-16" />
            ) : (
              outstandingCount.toLocaleString()
            )
          }
          subtitle="issued, awaiting payment"
        />
      </section>

      {/* ── Quick pivots ─────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-3 space-y-3">
        <div className="meta text-[var(--color-muted-foreground)]">// ENTRY POINTS</div>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <PivotCard
            to="/tenants"
            icon={Building2}
            title="Tenants"
            description="Provision, suspend, and inspect tenants. Watch provisioning runs in real time."
          />
          <PivotCard
            to="/users"
            icon={UsersRound}
            title="Users"
            description="Root-tenant operators. Tenant-scoped users live in each tenant's own dashboard."
          />
          <PivotCard
            to="/billing/plans"
            icon={Receipt}
            title="Billing"
            description="Plans, subscriptions, invoices. Drive pricing and the invoice state machine."
          />
          <PivotCard
            to="/billing/invoices"
            icon={FileText}
            title="Invoices"
            description="Cross-tenant ledger with filters. Issue, mark paid, void — all guarded server-side."
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
  title,
  description,
}: {
  to: string;
  icon: typeof Building2;
  title: string;
  description: string;
}) {
  return (
    <Link to={to} className="group block focus:outline-none">
      <Card interactive className="h-full">
        <CardHeader className="flex flex-row items-start justify-between gap-3">
          <div className="space-y-1">
            <span
              aria-hidden
              className="inline-grid h-9 w-9 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)] transition-colors group-hover:text-[var(--color-foreground)]"
            >
              <Icon className="h-4 w-4" />
            </span>
            <CardTitle className="font-display text-xl">{title}</CardTitle>
          </div>
          <ArrowUpRight className="h-4 w-4 text-[var(--color-muted-foreground)] transition-[color,transform] duration-200 group-hover:translate-x-0.5 group-hover:-translate-y-0.5 group-hover:text-[var(--color-accent-signal)]" />
        </CardHeader>
        <CardContent>
          <CardDescription>{description}</CardDescription>
        </CardContent>
      </Card>
    </Link>
  );
}
