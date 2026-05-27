import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Pencil, Plus } from "lucide-react";
import { getPlans, type BillingPlanDto } from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { KpiTile } from "@/components/kpi-tile";
import { ApiRequestError } from "@/lib/api-client";

// ─── helpers ──────────────────────────────────────────────────────────

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

function formatOverageRates(rates: BillingPlanDto["overageRates"], currency: string) {
  const entries = Object.entries(rates).filter(([, v]) => v && v > 0);
  if (entries.length === 0) return "—";
  return entries
    .map(([resource, rate]) => `${resource} ${formatMoney(rate ?? 0, currency)}`)
    .join(" · ");
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return "Failed to load plans.";
}

// ─── component ────────────────────────────────────────────────────────

export function PlansListPage() {
  const navigate = useNavigate();
  // Plans are typically a small set — fetch everything (including inactive) so
  // admins can see deactivated rows and the average-price KPI reflects truth.
  const query = useQuery({
    queryKey: ["billing", "plans", { includeInactive: true }],
    queryFn: () => getPlans(true),
  });

  // useMemo dependencies need a stable reference; wrap the optional list once
  // so the totals memo can depend on it without a fresh array on each render.
  const plans = useMemo<BillingPlanDto[]>(() => query.data ?? [], [query.data]);

  const totals = useMemo(() => {
    if (plans.length === 0) {
      return { count: 0, active: 0, averagePrice: 0, currency: "USD" };
    }
    const active = plans.filter((p) => p.isActive).length;
    const sum = plans.reduce((acc, p) => acc + p.monthlyBasePrice, 0);
    return {
      count: plans.length,
      active,
      averagePrice: sum / plans.length,
      currency: plans[0].currency,
    };
  }, [plans]);

  return (
    <div className="space-y-6">
      {/* KPI strip */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <KpiTile
          label="Plans"
          value={query.isLoading ? <Skeleton className="h-7 w-16" /> : totals.count}
          subtitle={`${totals.active} active`}
        />
        <KpiTile
          label="Active"
          value={query.isLoading ? <Skeleton className="h-7 w-16" /> : totals.active}
          subtitle={totals.count - totals.active > 0 ? `${totals.count - totals.active} inactive` : "all active"}
        />
        <KpiTile
          label="Average base"
          value={
            query.isLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              formatMoney(totals.averagePrice, totals.currency)
            )
          }
          subtitle="monthly subscription fee"
        />
      </div>

      {/* List card */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>All plans</CardTitle>
            <CardDescription>
              Pricing schedule used by tenant subscriptions and invoice generation.
            </CardDescription>
          </div>
          <Button onClick={() => navigate("/billing/plans/new")}>
            <Plus className="mr-1 h-4 w-4" /> New plan
          </Button>
        </CardHeader>
        <CardContent className="p-0">
          {query.isError && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)]">
              {describe(query.error)}
            </div>
          )}

          {query.isLoading ? (
            <ul className="divide-y divide-[var(--color-border)]">
              {Array.from({ length: 3 }).map((_, i) => (
                <li key={i} className="px-6 py-5">
                  <Skeleton className="h-5 w-1/3" />
                  <Skeleton className="mt-2 h-3 w-1/2" />
                </li>
              ))}
            </ul>
          ) : plans.length === 0 ? (
            <div className="px-6 py-10 text-center text-sm text-[var(--color-muted-foreground)]">
              No plans yet. Create your first plan to start charging tenants.
            </div>
          ) : (
            <ul>
              {plans.map((plan, i) => (
                <li
                  key={plan.id}
                  className={
                    "fsh-enter grid grid-cols-[1fr_auto] items-center gap-x-6 gap-y-1 border-t border-[var(--color-border)] px-6 py-4 transition-colors hover:bg-[var(--color-muted)] first:border-t-0"
                  }
                  style={{ animationDelay: `${Math.min(i, 6) * 30}ms` }}
                >
                  {/* Identity column */}
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <code className="rounded bg-[var(--color-surface-2)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight">
                        {plan.key}
                      </code>
                      <span className="font-display text-base font-semibold">{plan.name}</span>
                      {plan.isActive ? (
                        <Badge variant="success">Active</Badge>
                      ) : (
                        <Badge variant="muted">Inactive</Badge>
                      )}
                    </div>
                    <div className="mt-1 font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
                      currency {plan.currency} ·
                      {" "}
                      overage {formatOverageRates(plan.overageRates, plan.currency)}
                    </div>
                  </div>

                  {/* Right column — price + edit */}
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <div className="text-display text-lg font-semibold leading-none tabular-nums">
                        {formatMoney(plan.monthlyBasePrice, plan.currency)}
                      </div>
                      <div className="mt-1 font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                        per month
                      </div>
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`Edit ${plan.name}`}
                      onClick={() => navigate(`/billing/plans/${plan.id}`)}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
