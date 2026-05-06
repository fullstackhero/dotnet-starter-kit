import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  ArrowRight,
  ArrowUpRight,
  Calendar,
  ChevronRight,
  Gauge,
  Package,
  RefreshCw,
  ScrollText,
  Server,
  ShieldCheck,
  Sparkles,
  UsersRound,
  X,
} from "lucide-react";
import {
  getMySubscription,
  getUsageSnapshots,
  type SubscriptionDto,
  type UsageSnapshotDto,
} from "@/api/billing";
import {
  AuditEventType,
  AuditSeverity,
  AUDIT_EVENT_TYPE_LABELS,
  listAudits,
  type AuditSummaryDto,
} from "@/api/audits";
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

function periodProgress(now: Date = new Date()): number {
  // Fraction of the current month elapsed, 0..1.
  const day = now.getUTCDate();
  const last = new Date(now.getUTCFullYear(), now.getUTCMonth() + 1, 0).getUTCDate();
  return Math.min(1, Math.max(0, day / last));
}

function greetingFor(now: Date = new Date()): string {
  const h = now.getHours();
  if (h < 5) return "Good night";
  if (h < 12) return "Good morning";
  if (h < 17) return "Good afternoon";
  if (h < 22) return "Good evening";
  return "Good night";
}

function relativeTime(iso: string, now: number = Date.now()): string {
  const delta = Math.max(0, Math.floor((now - Date.parse(iso)) / 1000));
  if (delta < 60) return `${delta}s`;
  const m = Math.floor(delta / 60);
  if (m < 60) return `${m}m`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h`;
  return `${Math.floor(h / 24)}d`;
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
// Period progress ring — pure SVG. Track + arc + central percentage label
// + day countdown. The arc length is computed from the fraction so the
// stroke-dasharray + stroke-dashoffset combo animates smoothly via CSS.
// ────────────────────────────────────────────────────────────────────────

function PeriodRing({ fraction, daysLeft }: { fraction: number; daysLeft: number }) {
  const size = 96;
  const stroke = 7;
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const offset = c * (1 - Math.max(0, Math.min(1, fraction)));
  const pct = Math.round(fraction * 100);
  return (
    <div className="relative grid h-[96px] w-[96px] place-items-center">
      <svg
        viewBox={`0 0 ${size} ${size}`}
        width={size}
        height={size}
        aria-hidden
        className="-rotate-90"
      >
        <circle
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          stroke="var(--color-muted)"
          strokeWidth={stroke}
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          stroke="var(--color-primary)"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={c}
          strokeDashoffset={offset}
          style={{ transition: "stroke-dashoffset 700ms var(--ease-out-cubic)" }}
        />
      </svg>
      <div className="absolute inset-0 grid place-items-center text-center leading-none">
        <div>
          <div className="text-display text-lg font-semibold tabular-nums tracking-tight">
            {pct}%
          </div>
          <div className="mt-0.5 font-mono text-[9.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {daysLeft}d left
          </div>
        </div>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Recent audits preview — top 5 most recent audit events, severity-tinted.
// Click a row to deep-link into the audit trail page (the drawer there
// will pick up the filter/range and let the user expand).
// ────────────────────────────────────────────────────────────────────────

function recentSeverityColor(severity: number): string {
  if (severity >= AuditSeverity.Error) return "var(--color-destructive)";
  if (severity >= AuditSeverity.Warning) return "var(--color-warning)";
  if (severity >= AuditSeverity.Information) return "var(--color-info)";
  return "var(--color-muted-foreground)";
}

function recentEventTypeIcon(eventType: number): React.ComponentType<{ className?: string }> {
  if (eventType === AuditEventType.Security) return ShieldCheck;
  if (eventType === AuditEventType.Exception) return Activity;
  if (eventType === AuditEventType.EntityChange) return Server;
  return Activity;
}

function RecentAuditsCard() {
  const recentAudits = useQuery({
    queryKey: ["audits", "recent", "overview"],
    queryFn: ({ signal }) => {
      // 24h window, page size 5 — matches the visual capacity below.
      const to = new Date();
      const from = new Date(to.getTime() - 24 * 60 * 60 * 1000);
      return listAudits(
        { pageNumber: 1, pageSize: 5, fromUtc: from.toISOString(), toUtc: to.toISOString() },
        signal,
      );
    },
    staleTime: 30_000,
  });

  const items = recentAudits.data?.items ?? [];

  return (
    <Card className="fsh-enter fsh-enter-5 lg:col-span-7">
      <CardHeader className="flex flex-row items-end justify-between gap-3">
        <div>
          <CardTitle className="flex items-center gap-2">
            Recent operations
            <Badge variant="default">24h</Badge>
          </CardTitle>
          <CardDescription>
            Last {items.length || 5} audited actions. Tap a row to drill into the trail.
          </CardDescription>
        </div>
        <Link
          to="/system/audits"
          className="inline-flex items-center gap-1 text-[11px] font-medium text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
        >
          See all <ArrowUpRight className="h-3 w-3" />
        </Link>
      </CardHeader>
      <CardContent className="p-0">
        {recentAudits.isLoading ? (
          <ul className="space-y-2 px-6 pb-5">
            {[0, 1, 2, 3, 4].map((i) => (
              <li key={i} className="flex items-center gap-3">
                <Skeleton className="h-3 w-16" />
                <Skeleton className="h-3 w-32" />
                <Skeleton className="ml-auto h-3 w-20" />
              </li>
            ))}
          </ul>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center gap-2 px-6 py-10 text-center">
            <ScrollText className="h-4 w-4 text-[var(--color-muted-foreground)]" />
            <div className="text-sm font-medium tracking-tight">No recent activity</div>
            <p className="max-w-sm text-[11.5px] text-[var(--color-muted-foreground)]">
              Audits will appear here as the platform handles requests in the last 24h.
            </p>
          </div>
        ) : (
          <ul className="divide-y divide-[var(--color-border)]">
            {items.map((row) => (
              <RecentAuditRow key={row.id} row={row} />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

function RecentAuditRow({ row }: { row: AuditSummaryDto }) {
  const Icon = recentEventTypeIcon(row.eventType);
  const tone = recentSeverityColor(row.severity);
  return (
    <li>
      <Link
        to="/system/audits"
        className="group/row relative flex items-center gap-3 px-6 py-3 transition-colors hover:bg-[var(--color-accent)]"
      >
        <span
          aria-hidden
          className="absolute left-0 top-0 h-full w-[2px]"
          style={{ background: tone }}
        />
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md ring-1 ring-inset"
          style={{
            color: tone,
            background: `linear-gradient(135deg, oklch(from ${tone} l c h / 0.20), oklch(from ${tone} l c h / 0.02))`,
            boxShadow: `inset 0 0 0 1px oklch(from ${tone} l c h / 0.25)`,
          }}
        >
          <Icon className="h-3.5 w-3.5" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline gap-2">
            <span className="truncate text-[12.5px] font-medium tracking-tight">
              {row.source ?? AUDIT_EVENT_TYPE_LABELS[row.eventType] ?? "Event"}
            </span>
            <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              {AUDIT_EVENT_TYPE_LABELS[row.eventType]}
            </span>
          </div>
          <div className="mt-0.5 flex items-center gap-2 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
            <span>{row.userName ?? row.userId?.slice(0, 8) ?? "system"}</span>
            <span aria-hidden>·</span>
            <span className="tabular-nums">{relativeTime(row.occurredAtUtc)} ago</span>
          </div>
        </div>
        <ChevronRight className="h-3.5 w-3.5 text-[var(--color-muted-foreground)] transition-transform group-hover/row:translate-x-0.5" />
      </Link>
    </li>
  );
}

// ────────────────────────────────────────────────────────────────────────
// First-run panel — shown when the tenant has no active subscription and
// the user hasn't dismissed it. Surfaces the four most useful first
// destinations as polished action tiles. Auto-hides as soon as the tenant
// picks a plan; users can also opt out per-tenant via localStorage.
// ────────────────────────────────────────────────────────────────────────

const FIRST_RUN_DISMISSED_KEY = "fsh.firstrun.dismissed";

function dismissedKeyFor(tenantId: string | undefined): string {
  return `${FIRST_RUN_DISMISSED_KEY}:${tenantId ?? "_default"}`;
}

function readDismissed(tenantId: string | undefined): boolean {
  if (typeof window === "undefined") return false;
  try {
    return window.localStorage.getItem(dismissedKeyFor(tenantId)) === "true";
  } catch {
    return false;
  }
}

function writeDismissed(tenantId: string | undefined, value: boolean): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(dismissedKeyFor(tenantId), String(value));
  } catch {
    /* storage unavailable */
  }
}

type SetupTileSpec = {
  to: string;
  eyebrow: string;
  title: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  /** Tone OKLCH color var driving the icon plate + radial wash. */
  toneVar: string;
};

const SETUP_TILES: SetupTileSpec[] = [
  {
    to: "/invoices",
    eyebrow: "Step 01",
    title: "Pick a plan",
    description: "Choose a subscription to enable billing, quotas, and overage tracking.",
    icon: Sparkles,
    toneVar: "var(--color-primary)",
  },
  {
    to: "/identity/users",
    eyebrow: "Step 02",
    title: "Invite your team",
    description: "Add teammates, assign roles, and group them for permission scopes.",
    icon: UsersRound,
    toneVar: "var(--color-info)",
  },
  {
    to: "/catalog/products",
    eyebrow: "Step 03",
    title: "Browse the catalog",
    description: "See sample products, brands, and categories already wired up.",
    icon: Package,
    toneVar: "var(--color-success)",
  },
  {
    to: "/activity",
    eyebrow: "Step 04",
    title: "Watch live activity",
    description: "Server-Sent Events stream right into the dashboard in real time.",
    icon: Activity,
    toneVar: "var(--color-warning)",
  },
];

function FirstRunPanel({
  tenantName,
  tenantId,
  onDismiss,
}: {
  tenantName: string;
  tenantId: string | undefined;
  onDismiss: () => void;
}) {
  return (
    <section
      aria-labelledby="firstrun-heading"
      className="fsh-enter fsh-enter-1 relative overflow-hidden rounded-[20px] border border-[var(--color-border)] bg-[var(--color-surface-3)]"
    >
      {/* Atmospheric backdrop — two soft radial washes that converge in the
          centre. Pure decoration, pointer-events disabled so clicks pass
          through to the action tiles below. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background: `
            radial-gradient(60% 60% at 5% 0%, oklch(from var(--color-primary) l c h / 0.15), transparent 65%),
            radial-gradient(50% 50% at 100% 0%, oklch(0.700 0.155 195 / 0.08), transparent 70%),
            radial-gradient(45% 45% at 50% 110%, oklch(from var(--color-primary) l c h / 0.06), transparent 60%)
          `,
        }}
      />

      {/* Dismiss — top-right hairline X. */}
      <button
        type="button"
        onClick={() => {
          writeDismissed(tenantId, true);
          onDismiss();
        }}
        aria-label="Dismiss setup checklist"
        title="Skip for now"
        className="absolute right-3 top-3 z-10 grid h-7 w-7 cursor-pointer place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)] opacity-70 transition-all hover:opacity-100 hover:text-[var(--color-foreground)]"
      >
        <X className="h-3.5 w-3.5" />
      </button>

      <div className="relative px-6 py-7 sm:px-8 sm:py-9">
        <div className="max-w-3xl">
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-primary)]">
            ✦ Get started · 4 steps
          </span>
          <h2
            id="firstrun-heading"
            className="text-display mt-2 text-[28px] font-semibold leading-[1.05] tracking-[-0.02em] sm:text-[34px]"
          >
            Welcome to{" "}
            <span className="text-gradient-brand">{tenantName}</span>.
          </h2>
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            Your tenant is provisioned and ready. Here's where most teams start —
            pick a plan, invite collaborators, and let the live activity feed
            confirm everything's running.
          </p>
        </div>

        <ul className="mt-7 grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
          {SETUP_TILES.map((tile, idx) => (
            <li key={tile.to} className="fsh-enter" style={{ animationDelay: `${80 + idx * 60}ms` }}>
              <SetupTile spec={tile} />
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function SetupTile({ spec }: { spec: SetupTileSpec }) {
  const Icon = spec.icon;
  return (
    <Link
      to={spec.to}
      className={cn(
        "group/tile relative flex h-full flex-col gap-3 overflow-hidden rounded-2xl border bg-[var(--color-surface-2)] p-4",
        "border-[var(--color-border)] transition-all duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        "hover:-translate-y-0.5 hover:border-[var(--color-border-strong)] hover:bg-[var(--color-surface-3)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
      )}
    >
      {/* Tone wash — soft radial in the icon's tone, top-left. Fades up
          slightly on hover. */}
      <span
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-90 transition-opacity duration-[var(--duration-default)] group-hover/tile:opacity-100"
        style={{
          background: `radial-gradient(70% 60% at 0% 0%, oklch(from ${spec.toneVar} l c h / 0.10), transparent 70%)`,
        }}
      />

      <div className="relative flex items-start justify-between">
        <span
          aria-hidden
          className="grid h-9 w-9 place-items-center rounded-xl ring-1 ring-inset"
          style={{
            background: `linear-gradient(135deg, oklch(from ${spec.toneVar} l c h / 0.22), oklch(from ${spec.toneVar} l c h / 0.04))`,
            color: spec.toneVar,
            boxShadow: `inset 0 0 0 1px oklch(from ${spec.toneVar} l c h / 0.28)`,
          }}
        >
          <Icon className="h-4 w-4" />
        </span>
        <span className="font-mono text-[10px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
          {spec.eyebrow}
        </span>
      </div>

      <div className="relative">
        <div className="text-display text-[15px] font-semibold tracking-[-0.005em]">
          {spec.title}
        </div>
        <p className="mt-1 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
          {spec.description}
        </p>
      </div>

      <div className="relative mt-auto flex items-center gap-1.5 text-[11.5px] font-medium text-[var(--color-muted-foreground)] transition-colors group-hover/tile:text-[var(--color-foreground)]">
        Open
        <ArrowRight className="h-3 w-3 transition-transform duration-[var(--duration-default)] group-hover/tile:translate-x-0.5" />
      </div>
    </Link>
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

  // First-run state — show only when the tenant has no active subscription
  // and the user hasn't dismissed it for this tenant. Re-checks on tenant
  // change so switching tenants restores the panel.
  const tenantId = user?.tenant;
  const [dismissed, setDismissed] = useState<boolean>(() => readDismissed(tenantId));
  useEffect(() => {
    setDismissed(readDismissed(tenantId));
  }, [tenantId]);
  const showFirstRun =
    !dismissed && !subscription.isLoading && !subscription.data;

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
      {showFirstRun && (
        <FirstRunPanel
          tenantName={user?.tenant ?? "your tenant"}
          tenantId={tenantId}
          onDismiss={() => setDismissed(true)}
        />
      )}

      {/* ── Hero ────────────────────────────────────────────────────────
          Atmospheric cockpit-style block: time-of-day greeting + tenant
          chip + live presence on the left, period-progress ring + day
          countdown + refresh on the right. Background glows live in
          ::before/::after pseudo siblings via inline gradients. ── */}
      <section className="fsh-enter fsh-enter-1 relative overflow-hidden rounded-[20px] border border-[var(--color-border)] bg-[var(--color-surface-3)]">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0"
          style={{
            background: `
              radial-gradient(55% 65% at 0% 0%, oklch(from var(--color-primary) l c h / 0.16), transparent 65%),
              radial-gradient(45% 55% at 100% 0%, oklch(0.700 0.155 195 / 0.10), transparent 70%),
              radial-gradient(40% 40% at 100% 100%, oklch(from var(--color-primary) l c h / 0.06), transparent 65%)
            `,
          }}
        />

        <div className="relative grid grid-cols-1 gap-6 px-6 py-7 sm:px-8 sm:py-9 md:grid-cols-[1fr_auto] md:items-center">
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                Tenant
              </span>
              <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] font-medium text-[var(--color-primary)]">
                {user?.tenant ?? "—"}
              </code>
              <span aria-hidden className="h-3 w-px bg-[var(--color-border)]" />
              <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                {currentPeriodLabel()}
              </span>
            </div>

            <h1 className="text-display mt-3 text-[34px] font-semibold leading-[1.05] tracking-[-0.02em] sm:text-[42px]">
              {greetingFor()},{" "}
              <span className="text-gradient-brand">
                {(user?.name ?? user?.email?.split("@")[0] ?? "operator")
                  .toString()
                  .split(" ")[0]}
              </span>
              .
            </h1>

            <p className="mt-2.5 max-w-xl text-sm leading-relaxed text-[var(--color-muted-foreground)]">
              Live telemetry for{" "}
              <span className="font-medium text-[var(--color-foreground)]">
                {user?.tenant ?? "your tenant"}
              </span>
              . The pulse below reflects the SSE stream;{" "}
              <span className="tabular-nums font-mono text-[var(--color-foreground)]">
                {formatNumber(eventCount)}
              </span>{" "}
              events received this session.
            </p>

            <div className="mt-4 inline-flex items-center gap-2 rounded-full border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2.5 py-1 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
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
              <span className="capitalize tracking-[0.14em] text-[var(--color-foreground)]">
                {sseStatus === "connected" ? "Stream live" : sseStatus}
              </span>
            </div>
          </div>

          <div className="flex flex-row items-center gap-5 md:flex-col md:items-end">
            <PeriodRing fraction={periodProgress()} daysLeft={daysLeftInMonth()} />
            <Button variant="outline" size="sm" disabled={refreshing} onClick={onRefresh}>
              <RefreshCw
                className={cn("mr-1.5 h-3.5 w-3.5", refreshing && "animate-spin")}
              />
              Refresh
            </Button>
          </div>
        </div>
      </section>

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

      {/* ── Recent operations + Live stream ────────────────────────────
          Audited ops history (24h) on the left, raw SSE stream on the
          right. The audit panel is the long-term ledger; the live feed
          is the heart-rate monitor. */}
      <section className="grid grid-cols-1 gap-4 lg:grid-cols-12">
        <RecentAuditsCard />
        <div className="fsh-enter fsh-enter-5 lg:col-span-5">
          <LiveFeed limit={8} />
        </div>
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
