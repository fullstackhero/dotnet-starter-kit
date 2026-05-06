import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  ArrowUpRight,
  Calendar,
  Gauge,
  RefreshCw,
  Server,
  Sparkles,
} from "lucide-react";
import {
  getMySubscription,
  getUsageSnapshots,
  type SubscriptionDto,
  type UsageSnapshotDto,
} from "@/api/billing";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { LiveFeed } from "@/components/sse/live-feed";
import { useAuth } from "@/auth/use-auth";
import { useSse } from "@/sse/sse-context";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────────
// Shaping helpers — pure, tested via memoization at the call sites.
// ────────────────────────────────────────────────────────────────────────

type UsageRowVm = {
  resource: string;
  used: number;
  limit: number;
  overage: number;
  utilization: number;
};

const numberFmt = new Intl.NumberFormat("en-US");
const formatNumber = (n: number) => numberFmt.format(n);

function toUsageRows(snapshots: UsageSnapshotDto[]): UsageRowVm[] {
  const now = new Date();
  const cy = now.getUTCFullYear();
  const cm = now.getUTCMonth() + 1;
  return snapshots
    .filter((s) => s.periodYear === cy && s.periodMonth === cm)
    .map((s) => ({
      resource: String(s.resource),
      used: s.usedUnits,
      limit: s.limitUnits,
      overage: s.overage,
      utilization: s.limitUnits > 0 ? Math.min(100, (s.usedUnits / s.limitUnits) * 100) : 0,
    }))
    .sort((a, b) => b.utilization - a.utilization);
}

function daysLeftInMonth(now: Date = new Date()): number {
  const last = new Date(now.getUTCFullYear(), now.getUTCMonth() + 1, 0).getUTCDate();
  return Math.max(0, last - now.getUTCDate());
}

function currentPeriodLabel(now: Date = new Date()): string {
  return now.toLocaleDateString("en-US", { month: "long", year: "numeric", timeZone: "UTC" });
}

function subscriptionTone(status: SubscriptionDto["status"] | undefined) {
  if (status === "Active") return "success" as const;
  if (status === "Canceled") return "warning" as const;
  if (status === "Expired") return "danger" as const;
  return "default" as const;
}

// ────────────────────────────────────────────────────────────────────────
// Subcomponents — small, focused, all client-pure. Extracted so the page
// reads top-down without nested closures.
// ────────────────────────────────────────────────────────────────────────

type KpiTileProps = {
  label: string;
  value: React.ReactNode;
  subtitle?: React.ReactNode;
  trailing?: React.ReactNode;
  icon?: React.ComponentType<{ className?: string }>;
  href?: string;
  className?: string;
};

function KpiTile({ label, value, subtitle, trailing, icon: Icon, href, className }: KpiTileProps) {
  const body = (
    <Card interactive className={cn("group/tile h-full", className)}>
      <CardContent className="px-5 pb-5 pt-5">
        <div className="flex items-center justify-between">
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {label}
          </span>
          {Icon && <Icon className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" aria-hidden />}
        </div>
        <div className="mt-3 flex items-end justify-between gap-3">
          <div className="text-display text-2xl font-semibold leading-none tabular-nums">
            {value}
          </div>
          {trailing}
        </div>
        {subtitle && (
          <div className="mt-2 text-xs leading-relaxed text-[var(--color-muted-foreground)]">
            {subtitle}
          </div>
        )}
        {href && (
          <ArrowUpRight
            aria-hidden
            className={cn(
              "absolute right-4 top-4 h-3.5 w-3.5 text-[var(--color-muted-foreground)]",
              "opacity-0 transition-opacity duration-[var(--duration-default)]",
              "group-hover/tile:opacity-100",
            )}
          />
        )}
      </CardContent>
    </Card>
  );
  return href ? (
    <a href={href} className="relative block">
      {body}
    </a>
  ) : (
    body
  );
}

function UsageRow({ row, animationDelayMs }: { row: UsageRowVm; animationDelayMs: number }) {
  const overUtilized = row.utilization >= 80;
  const overage = row.overage > 0;
  return (
    <li
      className={cn(
        "fsh-enter group/row grid grid-cols-[1fr_auto] items-center gap-x-6 gap-y-2 py-3.5",
        "border-t border-[var(--color-border)] first:border-t-0",
      )}
      style={{ animationDelay: `${animationDelayMs}ms` }}
    >
      <div className="flex items-center gap-2.5">
        <span className="font-mono text-[11px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
          resource
        </span>
        <span className="text-sm font-medium tracking-tight text-[var(--color-foreground)]">
          {row.resource}
        </span>
        {overage && (
          <Badge variant="danger">+{formatNumber(row.overage)} overage</Badge>
        )}
      </div>

      <div className="text-right tabular-nums">
        <div className="text-sm font-semibold tracking-tight">
          {formatNumber(row.used)}
          <span className="ml-1 font-normal text-[var(--color-muted-foreground)]">
            / {formatNumber(row.limit)}
          </span>
        </div>
        <div className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
          {row.utilization.toFixed(1)}%
        </div>
      </div>

      <div className="col-span-2">
        <div className="relative h-1.5 overflow-hidden rounded-full bg-[var(--color-muted)]">
          <div
            className={cn(
              "h-full rounded-full transition-[width] duration-[700ms] ease-[var(--ease-out-cubic)]",
              overage
                ? "bg-[var(--color-destructive)]"
                : overUtilized
                  ? "bg-[var(--color-warning)]"
                  : "bg-[var(--color-primary)]",
            )}
            style={{ width: `${row.utilization}%` }}
          />
        </div>
      </div>
    </li>
  );
}

function UsageSkeleton() {
  return (
    <ul className="space-y-3 px-6 pb-5">
      {[0, 1, 2].map((i) => (
        <li key={i} className="space-y-2 py-2">
          <div className="flex items-center justify-between">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-4 w-24" />
          </div>
          <Skeleton className="h-1.5 w-full" />
        </li>
      ))}
    </ul>
  );
}

type SubscriptionPanelProps = {
  data: SubscriptionDto | null | undefined;
  loading: boolean;
};

function SubscriptionPanel({ data, loading }: SubscriptionPanelProps) {
  if (loading) {
    return (
      <div className="space-y-4 px-6 pb-5 pt-1">
        <Skeleton className="h-7 w-3/5" />
        <Skeleton className="h-4 w-2/5" />
        <Skeleton className="h-4 w-4/5" />
        <Skeleton className="h-4 w-3/5" />
      </div>
    );
  }
  if (!data) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 px-6 pb-7 pt-3 text-center">
        <span
          aria-hidden
          className="grid h-9 w-9 place-items-center rounded-full bg-[var(--color-muted)]"
        >
          <Sparkles className="h-4 w-4 text-[var(--color-muted-foreground)]" />
        </span>
        <div>
          <div className="text-sm font-medium tracking-tight">No active subscription</div>
          <div className="mt-1 text-xs text-[var(--color-muted-foreground)]">
            Pick a plan to enable billing.
          </div>
        </div>
        <Button variant="soft" size="sm">Choose plan</Button>
      </div>
    );
  }

  const tone = subscriptionTone(data.status);
  return (
    <div className="space-y-5 px-6 pb-5 pt-1">
      <div className="flex items-center gap-3">
        <span className="text-display text-2xl font-semibold tracking-tight">
          {data.planKey}
        </span>
        <Badge variant={tone}>
          {data.status === "Active" && (
            <span
              aria-hidden
              className="pulse-dot inline-block h-1.5 w-1.5 rounded-full"
              style={{ backgroundColor: "var(--color-success)", color: "var(--color-success)" }}
            />
          )}
          {data.status}
        </Badge>
      </div>

      <dl className="space-y-3 text-sm">
        <DefRow
          label="Started"
          value={
            <span className="font-mono tabular-nums">
              {new Date(data.startUtc).toLocaleDateString("en-US", {
                month: "short",
                day: "2-digit",
                year: "numeric",
              })}
            </span>
          }
        />
        <DefRow
          label="Ends"
          value={
            data.endUtc ? (
              <span className="font-mono tabular-nums">
                {new Date(data.endUtc).toLocaleDateString("en-US", {
                  month: "short",
                  day: "2-digit",
                  year: "numeric",
                })}
              </span>
            ) : (
              <span className="font-mono text-[var(--color-muted-foreground)]">open-ended</span>
            )
          }
        />
        <DefRow
          label="Plan ID"
          value={
            <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px]">
              {data.planId.slice(0, 8)}…
            </code>
          }
        />
      </dl>
    </div>
  );
}

function DefRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <dt className="font-mono text-[11px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd>{value}</dd>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

export function OverviewPage() {
  const { user } = useAuth();
  const { status: sseStatus, eventCount } = useSse();

  const usage = useQuery({
    queryKey: ["billing", "usage"],
    queryFn: () => getUsageSnapshots(),
    staleTime: 60_000,
  });

  const subscription = useQuery({
    queryKey: ["billing", "subscription", "me"],
    queryFn: () => getMySubscription(),
    staleTime: 60_000,
  });

  const rows = useMemo(
    () => (usage.data ? toUsageRows(usage.data) : []),
    [usage.data],
  );

  const totalsView = useMemo(() => {
    if (!rows.length) {
      return { resourceCount: 0, avgUtilization: 0, overage: 0 };
    }
    const overage = rows.reduce((sum, r) => sum + r.overage, 0);
    const avg = rows.reduce((sum, r) => sum + r.utilization, 0) / rows.length;
    return { resourceCount: rows.length, avgUtilization: avg, overage };
  }, [rows]);

  const refreshing = usage.isFetching || subscription.isFetching;
  const onRefresh = () => {
    void usage.refetch();
    void subscription.refetch();
  };

  return (
    <div className="space-y-7">
      {/* ── Header ──────────────────────────────────────────────────── */}
      <header className="fsh-enter fsh-enter-1 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
              Tenant
            </span>
            <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] font-medium text-[var(--color-primary)]">
              {user?.tenant ?? "—"}
            </code>
          </div>
          <h1 className="text-display mt-2 text-[28px] font-semibold leading-tight">
            Overview
          </h1>
          <p className="mt-1 text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            Live telemetry for{" "}
            <span className="font-medium text-[var(--color-foreground)]">
              {user?.name ?? user?.email ?? "your tenant"}
            </span>
            . Period {currentPeriodLabel()}.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={refreshing} onClick={onRefresh}>
            <RefreshCw
              className={cn("mr-1.5 h-3.5 w-3.5", refreshing && "animate-spin")}
            />
            Refresh
          </Button>
        </div>
      </header>

      {/* ── KPI strip ───────────────────────────────────────────────── */}
      <section
        aria-label="Key metrics"
        className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4"
      >
        <div className="fsh-enter fsh-enter-2">
          <KpiTile
            label="Plan"
            icon={Server}
            value={
              subscription.isLoading ? (
                <Skeleton className="h-7 w-20" />
              ) : (
                subscription.data?.planKey ?? "—"
              )
            }
            subtitle={
              subscription.data ? (
                <>
                  Subscription <Badge variant={subscriptionTone(subscription.data.status)}>{subscription.data.status}</Badge>
                </>
              ) : (
                "No active subscription"
              )
            }
          />
        </div>

        <div className="fsh-enter fsh-enter-2">
          <KpiTile
            label="Period"
            icon={Calendar}
            value={currentPeriodLabel()}
            subtitle={
              <span className="font-mono">
                {daysLeftInMonth()} days remaining
              </span>
            }
          />
        </div>

        <div className="fsh-enter fsh-enter-3">
          <KpiTile
            label="Resources"
            icon={Gauge}
            value={
              usage.isLoading ? (
                <Skeleton className="h-7 w-12" />
              ) : (
                formatNumber(totalsView.resourceCount)
              )
            }
            subtitle={
              <>
                avg utilization{" "}
                <span className="font-mono tabular-nums text-[var(--color-foreground)]">
                  {totalsView.avgUtilization.toFixed(0)}%
                </span>
                {totalsView.overage > 0 && (
                  <>
                    {" · "}
                    <span className="text-[var(--color-destructive)]">
                      {formatNumber(totalsView.overage)} overage
                    </span>
                  </>
                )}
              </>
            }
          />
        </div>

        <div className="fsh-enter fsh-enter-3">
          <KpiTile
            label="Live events"
            icon={Activity}
            href="/activity"
            value={
              <span className="tabular-nums">{formatNumber(eventCount)}</span>
            }
            subtitle={
              <span className="inline-flex items-center gap-1.5">
                <span
                  aria-hidden
                  className={cn(
                    "inline-block h-1.5 w-1.5 rounded-full",
                    sseStatus === "connected" && "pulse-dot",
                  )}
                  style={{
                    backgroundColor:
                      sseStatus === "connected"
                        ? "var(--color-success)"
                        : sseStatus === "error"
                          ? "var(--color-destructive)"
                          : "var(--color-muted-foreground)",
                    color:
                      sseStatus === "connected" ? "var(--color-success)" : undefined,
                  }}
                />
                <span className="capitalize">{sseStatus}</span>
              </span>
            }
            trailing={
              sseStatus === "connected" ? (
                <Badge variant="success">live</Badge>
              ) : null
            }
          />
        </div>
      </section>

      {/* ── Usage + Subscription ─────────────────────────────────────── */}
      <section className="grid grid-cols-1 gap-4 lg:grid-cols-12">
        <Card className="fsh-enter fsh-enter-4 lg:col-span-8">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              Quota usage
              {totalsView.overage > 0 && (
                <Badge variant="danger">overage</Badge>
              )}
            </CardTitle>
            <CardDescription>
              Current-month consumption against plan limits, sorted by utilization.
            </CardDescription>
          </CardHeader>
          <CardContent className="px-6 pb-5 pt-1">
            {usage.isLoading ? (
              <UsageSkeleton />
            ) : usage.isError ? (
              <EmptyState
                title="Couldn't load usage"
                description="The usage endpoint returned an error. Try refreshing."
              />
            ) : rows.length === 0 ? (
              <EmptyState
                title="No usage captured yet"
                description="Activity will appear here as the backend records snapshots for this period."
              />
            ) : (
              <ul className="-mt-1">
                {rows.map((row, idx) => (
                  <UsageRow key={row.resource} row={row} animationDelayMs={50 * idx} />
                ))}
              </ul>
            )}
          </CardContent>
        </Card>

        <Card className="fsh-enter fsh-enter-4 lg:col-span-4">
          <CardHeader>
            <CardTitle>Subscription</CardTitle>
            <CardDescription>Current plan and validity window.</CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            <SubscriptionPanel data={subscription.data} loading={subscription.isLoading} />
          </CardContent>
        </Card>
      </section>

      {/* ── Live activity ────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-5">
        <LiveFeed limit={20} />
      </section>
    </div>
  );
}

function EmptyState({ title, description }: { title: string; description: string }) {
  // Inline status panel inside the Quota card. Smaller-scale than the
  // shared `EmptyState` "plinth" primitive on purpose — this is a status
  // panel, not a CTA pulse, so the chrome stays restrained.
  return (
    <div className="flex flex-col items-center justify-center gap-2.5 py-12 text-center">
      <span
        aria-hidden
        className={cn(
          "grid h-9 w-9 place-items-center rounded-lg",
          "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.16),oklch(from_var(--color-primary)_l_c_h_/_0.02))]",
          "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)]",
        )}
      >
        <Gauge className="h-4 w-4 text-[var(--color-primary)]" />
      </span>
      <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        Quota status
      </span>
      <div className="text-display text-base font-semibold tracking-tight">{title}</div>
      <p className="max-w-sm text-xs leading-relaxed text-[var(--color-muted-foreground)]">
        {description}
      </p>
    </div>
  );
}
