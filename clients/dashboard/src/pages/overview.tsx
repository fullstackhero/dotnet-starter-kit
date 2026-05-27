import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  ArrowRight,
  ArrowUpRight,
  Calendar,
  ChevronRight,
  CreditCard,
  Gauge,
  Package,
  RefreshCw,
  ScrollText,
  Server,
  ShieldCheck,
  Sparkles,
  UsersRound,
  Wifi,
  WifiOff,
  X,
  Zap,
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
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { EntityDetailSection } from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { useSseEvents, useSseStatus, type SseEvent, type SseStatus } from "@/sse/sse-context";
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

/**
 * Time-of-day greeting. Three buckets — morning (<12), afternoon (<17),
 * evening (rest). Mirrors the dentalOS dashboard greeting helper so the
 * tone of voice matches across products.
 */
function getGreeting(): "Good morning" | "Good afternoon" | "Good evening" {
  const hour = new Date().getHours();
  if (hour < 12) return "Good morning";
  if (hour < 17) return "Good afternoon";
  return "Good evening";
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

const timeFmt = new Intl.DateTimeFormat("en-US", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});
function formatClock(ts: number) {
  return timeFmt.format(new Date(ts));
}

function eventTone(type: string): "default" | "success" | "warning" | "danger" | "brand" {
  const t = type.toLowerCase();
  if (t.includes("fail") || t.includes("error") || t.includes("revoke")) return "danger";
  if (t.includes("warn") || t.includes("retry")) return "warning";
  if (t.includes("login") || t.includes("issued") || t.includes("created")) return "success";
  if (t.includes("token") || t.includes("auth")) return "brand";
  return "default";
}

// ────────────────────────────────────────────────────────────────────────
// StatCard — flat, calm, four-up. Icon tile on the left, label + big
// tabular-nums value stacked on the right. Tone tints the icon plate.
// ────────────────────────────────────────────────────────────────────────

type StatTone = "primary" | "success" | "warning" | "info";

const STAT_TONE_BG: Record<StatTone, string> = {
  primary: "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]",
  success: "bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]",
  warning: "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.12)] text-[var(--color-warning)]",
  info: "bg-[oklch(from_var(--color-info)_l_c_h_/_0.10)] text-[var(--color-info)]",
};

function StatCard({
  index,
  label,
  value,
  sublabel,
  icon: Icon,
  tone,
  href,
}: {
  index: number;
  label: string;
  value: React.ReactNode;
  sublabel?: React.ReactNode;
  icon: React.ComponentType<{ className?: string }>;
  tone: StatTone;
  href?: string;
}) {
  const body = (
    <div
      className={cn(
        "fsh-enter group/stat flex h-full items-start gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-4 py-3.5 shadow-xs",
        "transition-colors duration-200 hover:border-[var(--color-border-strong)]",
      )}
      style={{ animationDelay: `${index * 50}ms` }}
    >
      <span
        aria-hidden
        className={cn(
          "grid size-9 shrink-0 place-items-center rounded-lg",
          STAT_TONE_BG[tone],
        )}
      >
        <Icon className="size-4" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
          {label}
        </p>
        <p className="mt-1 font-display text-[20px] font-bold leading-none tracking-tight tabular-nums text-foreground sm:text-[22px]">
          {value}
        </p>
        {sublabel && (
          <p className="mt-1.5 truncate text-[11px] text-muted-foreground">
            {sublabel}
          </p>
        )}
      </div>
      {href && (
        <ArrowUpRight
          aria-hidden
          className="size-3.5 shrink-0 text-muted-foreground opacity-0 transition-opacity group-hover/stat:opacity-100"
        />
      )}
    </div>
  );
  return href ? (
    <Link to={href} className="block">
      {body}
    </Link>
  ) : (
    body
  );
}

// ────────────────────────────────────────────────────────────────────────
// Usage row — tightened single-line layout. Label · used/limit · bar.
// ────────────────────────────────────────────────────────────────────────

function UsageRow({ row }: { row: UsageRowVm }) {
  const overUtilized = row.utilization >= 80;
  const overage = row.overage > 0;
  return (
    <li className="grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-1.5 border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] py-2.5 first:border-t-0 first:pt-0">
      <div className="flex min-w-0 items-center gap-2">
        <span className="truncate text-[12.5px] font-medium tracking-tight text-foreground">
          {row.resource}
        </span>
        {overage && <Badge variant="danger">+{formatNumber(row.overage)}</Badge>}
      </div>

      <div className="text-right tabular-nums">
        <span className="text-[12.5px] font-semibold tracking-tight text-foreground">
          {formatNumber(row.used)}
        </span>
        <span className="ml-1 text-[11.5px] font-normal text-muted-foreground">
          / {formatNumber(row.limit)}
        </span>
        <span className="ml-2 text-[11px] tabular-nums text-muted-foreground">
          {row.utilization.toFixed(0)}%
        </span>
      </div>

      <div className="col-span-2">
        <div className="relative h-1 overflow-hidden rounded-full bg-[var(--color-muted)]">
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
    <ul className="space-y-3">
      {[0, 1, 2].map((i) => (
        <li key={i} className="space-y-2 py-1.5">
          <div className="flex items-center justify-between">
            <Skeleton className="h-3.5 w-32" />
            <Skeleton className="h-3.5 w-24" />
          </div>
          <Skeleton className="h-1 w-full" />
        </li>
      ))}
    </ul>
  );
}

function UsageEmpty({ title, description }: { title: string; description: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-8 text-center">
      <span
        aria-hidden
        className="grid size-8 place-items-center rounded-lg bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)]"
      >
        <Gauge className="size-3.5 text-[var(--color-primary)]" />
      </span>
      <div className="text-[13px] font-semibold tracking-tight text-foreground">{title}</div>
      <p className="max-w-sm text-[11.5px] leading-relaxed text-muted-foreground">
        {description}
      </p>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Subscription side card — plan name + status badge, validity window,
// and current-period progress bar.
// ────────────────────────────────────────────────────────────────────────

function SubscriptionBody({
  data,
  loading,
}: {
  data: SubscriptionDto | null | undefined;
  loading: boolean;
}) {
  if (loading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-6 w-3/5" />
        <Skeleton className="h-3 w-2/5" />
        <Skeleton className="h-3 w-4/5" />
        <Skeleton className="h-2 w-full" />
      </div>
    );
  }
  if (!data) {
    return (
      <div className="flex flex-col items-start gap-3">
        <div>
          <div className="text-[13px] font-semibold tracking-tight text-foreground">
            No active subscription
          </div>
          <p className="mt-1 text-[11.5px] leading-relaxed text-muted-foreground">
            Pick a plan to enable billing, quotas, and overage tracking.
          </p>
        </div>
        <Button asChild variant="soft" size="sm">
          <Link to="/invoices">Choose plan</Link>
        </Button>
      </div>
    );
  }

  const tone = subscriptionTone(data.status);
  const progressPct = Math.round(periodProgress() * 100);
  const daysLeft = daysLeftInMonth();

  const dateFmt: Intl.DateTimeFormatOptions = {
    month: "short",
    day: "2-digit",
    year: "numeric",
  };

  return (
    <div className="space-y-4">
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-display text-[22px] font-bold tracking-tight text-foreground">
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

      <dl className="space-y-1.5 text-[12px]">
        <div className="flex items-center justify-between gap-3">
          <dt className="text-muted-foreground">Started</dt>
          <dd className="tabular-nums text-foreground">
            {new Date(data.startUtc).toLocaleDateString("en-US", dateFmt)}
          </dd>
        </div>
        <div className="flex items-center justify-between gap-3">
          <dt className="text-muted-foreground">Ends</dt>
          <dd className="tabular-nums text-foreground">
            {data.endUtc
              ? new Date(data.endUtc).toLocaleDateString("en-US", dateFmt)
              : "open-ended"}
          </dd>
        </div>
      </dl>

      <div className="space-y-1.5">
        <div className="flex items-center justify-between text-[11px]">
          <span className="text-muted-foreground">Current period</span>
          <span className="tabular-nums text-foreground">
            {progressPct}% · {daysLeft}d left
          </span>
        </div>
        <div className="h-1 overflow-hidden rounded-full bg-[var(--color-muted)]">
          <div
            className="h-full rounded-full bg-[var(--color-primary)] transition-[width] duration-[700ms] ease-[var(--ease-out-cubic)]"
            style={{ width: `${progressPct}%` }}
          />
        </div>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// System status — SSE pulse + event count + connection state.
// ────────────────────────────────────────────────────────────────────────

function SystemStatusBody({
  sseStatus,
  eventCount,
}: {
  sseStatus: SseStatus;
  eventCount: number;
}) {
  const live = sseStatus === "connected";
  const errored = sseStatus === "error";
  const Icon = live ? Wifi : WifiOff;
  const tone: "success" | "danger" | "default" = live ? "success" : errored ? "danger" : "default";

  const toneBg =
    tone === "success"
      ? "bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]"
      : tone === "danger"
        ? "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] text-[var(--color-destructive)]"
        : "bg-[var(--color-muted)] text-muted-foreground";

  return (
    <div className="space-y-3">
      <div className="flex items-start gap-3">
        <span
          aria-hidden
          className={cn(
            "relative grid size-9 shrink-0 place-items-center rounded-lg",
            toneBg,
          )}
        >
          <Icon className="size-4" />
          {live && (
            <span
              aria-hidden
              className="pulse-dot absolute -right-0.5 -top-0.5 size-2 rounded-full"
              style={{
                backgroundColor: "var(--color-success)",
                color: "var(--color-success)",
              }}
            />
          )}
        </span>
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <span className="text-[13px] font-semibold capitalize tracking-tight text-foreground">
              {live ? "Stream live" : sseStatus}
            </span>
            {live && <Badge variant="success">SSE</Badge>}
            {errored && <Badge variant="danger">offline</Badge>}
          </div>
          <p className="mt-0.5 text-[11.5px] text-muted-foreground">
            {live
              ? "Backend events are flowing in real time."
              : errored
                ? "Stream disconnected. Events will queue once it recovers."
                : "Waiting for the stream to come online."}
          </p>
        </div>
      </div>

      <div className="flex items-center justify-between border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] pt-3">
        <span className="text-[11px] uppercase tracking-wider text-muted-foreground">
          Events this session
        </span>
        <span className="font-display text-[16px] font-bold tabular-nums text-foreground">
          {formatNumber(eventCount)}
        </span>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Recent audits — top 5 of the last 24h. Severity stripe, type icon,
// actor + relative timestamp. Click row to deep-link into the trail.
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

function RecentAuditsBody() {
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

  if (recentAudits.isLoading) {
    return (
      <ul className="space-y-2.5">
        {[0, 1, 2, 3, 4].map((i) => (
          <li key={i} className="flex items-center gap-3">
            <Skeleton className="size-7 rounded-md" />
            <Skeleton className="h-3 w-32" />
            <Skeleton className="ml-auto h-3 w-16" />
          </li>
        ))}
      </ul>
    );
  }

  if (items.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 py-6 text-center">
        <ScrollText className="size-4 text-muted-foreground" />
        <div className="text-[13px] font-semibold tracking-tight text-foreground">
          No recent activity
        </div>
        <p className="max-w-sm text-[11.5px] text-muted-foreground">
          Audited actions in the last 24 hours will appear here.
        </p>
      </div>
    );
  }

  return (
    <ul className="-my-1 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
      {items.map((row) => (
        <RecentAuditRow key={row.id} row={row} />
      ))}
    </ul>
  );
}

function RecentAuditRow({ row }: { row: AuditSummaryDto }) {
  const Icon = recentEventTypeIcon(row.eventType);
  const tone = recentSeverityColor(row.severity);
  return (
    <li>
      <Link
        to="/system/audits"
        className="group/row -mx-1 flex items-center gap-3 rounded-md px-1 py-2.5 transition-colors hover:bg-[var(--color-accent)]"
      >
        <span
          aria-hidden
          className="grid size-7 shrink-0 place-items-center rounded-md"
          style={{
            color: tone,
            background: `oklch(from ${tone} l c h / 0.10)`,
          }}
        >
          <Icon className="size-3.5" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="truncate text-[12.5px] font-medium tracking-tight text-foreground">
            {row.source ?? AUDIT_EVENT_TYPE_LABELS[row.eventType] ?? "Event"}
          </div>
          <div className="mt-0.5 flex items-center gap-1.5 text-[11px] text-muted-foreground">
            <span className="truncate">
              {row.userName ?? row.userId?.slice(0, 8) ?? "system"}
            </span>
            <span aria-hidden>·</span>
            <span className="tabular-nums">{relativeTime(row.occurredAtUtc)} ago</span>
          </div>
        </div>
        <ChevronRight className="size-3.5 shrink-0 text-muted-foreground transition-transform group-hover/row:translate-x-0.5" />
      </Link>
    </li>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Quick actions — four shortcut tiles to the most useful destinations.
// ────────────────────────────────────────────────────────────────────────

type QuickAction = {
  to: string;
  title: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  tone: StatTone;
};

const QUICK_ACTIONS: QuickAction[] = [
  {
    to: "/identity/users",
    title: "Invite users",
    description: "Add teammates, assign roles.",
    icon: UsersRound,
    tone: "info",
  },
  {
    to: "/catalog/products",
    title: "Browse catalog",
    description: "Products, brands, categories.",
    icon: Package,
    tone: "success",
  },
  {
    to: "/invoices",
    title: "Subscription",
    description: "Plans, invoices, usage.",
    icon: CreditCard,
    tone: "primary",
  },
  {
    to: "/activity",
    title: "Live activity",
    description: "Real-time event stream.",
    icon: Activity,
    tone: "warning",
  },
];

function QuickActionsBody() {
  return (
    <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
      {QUICK_ACTIONS.map((a) => (
        <Link
          key={a.to}
          to={a.to}
          className={cn(
            "group/qa flex items-start gap-2.5 rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-3",
            "transition-colors duration-200 hover:border-[var(--color-border-strong)] hover:bg-[var(--color-accent)]",
          )}
        >
          <span
            aria-hidden
            className={cn(
              "grid size-8 shrink-0 place-items-center rounded-md",
              STAT_TONE_BG[a.tone],
            )}
          >
            <a.icon className="size-3.5" />
          </span>
          <div className="min-w-0 flex-1">
            <div className="text-[12.5px] font-semibold tracking-tight text-foreground">
              {a.title}
            </div>
            <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">
              {a.description}
            </p>
          </div>
          <ArrowRight className="size-3 shrink-0 text-muted-foreground opacity-0 transition-all group-hover/qa:translate-x-0.5 group-hover/qa:opacity-100" />
        </Link>
      ))}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Live feed body — slim version of the LiveFeed widget that drops the
// outer Card chrome so it can be embedded inside an EntityDetailSection.
// ────────────────────────────────────────────────────────────────────────

function LiveFeedBody({ events }: { events: SseEvent[] }) {
  const visible = useMemo(() => events.slice(0, 5), [events]);

  if (visible.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 py-6 text-center">
        <Activity className="size-4 text-muted-foreground" />
        <div className="text-[13px] font-semibold tracking-tight text-foreground">
          Listening for activity
        </div>
        <p className="max-w-sm text-[11.5px] text-muted-foreground">
          Events will appear here as the backend publishes them.
        </p>
      </div>
    );
  }

  return (
    <ul className="-my-1 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
      {visible.map((ev) => (
        <li key={ev.id} className="flex items-center gap-3 py-2.5">
          <span className="shrink-0 font-mono text-[10.5px] tabular-nums text-muted-foreground">
            {formatClock(ev.receivedAt)}
          </span>
          <Badge variant={eventTone(ev.type)}>{ev.type}</Badge>
        </li>
      ))}
    </ul>
  );
}

// ────────────────────────────────────────────────────────────────────────
// First-run setup card — shown when the tenant has no active subscription
// and the user hasn't dismissed it. Auto-hides as soon as the tenant
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
  step: string;
  title: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  tone: StatTone;
};

const SETUP_TILES: SetupTileSpec[] = [
  {
    to: "/invoices",
    step: "01",
    title: "Pick a plan",
    description: "Enable billing, quotas, and overage tracking.",
    icon: Sparkles,
    tone: "primary",
  },
  {
    to: "/identity/users",
    step: "02",
    title: "Invite your team",
    description: "Add teammates, assign roles, and group them.",
    icon: UsersRound,
    tone: "info",
  },
  {
    to: "/catalog/products",
    step: "03",
    title: "Browse catalog",
    description: "Sample products, brands, categories ready to go.",
    icon: Package,
    tone: "success",
  },
  {
    to: "/activity",
    step: "04",
    title: "Watch live",
    description: "SSE stream right into the dashboard.",
    icon: Activity,
    tone: "warning",
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
      className="fsh-enter relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs"
    >
      <button
        type="button"
        onClick={() => {
          writeDismissed(tenantId, true);
          onDismiss();
        }}
        aria-label="Dismiss setup checklist"
        title="Skip for now"
        className="absolute right-3 top-3 z-10 grid size-7 cursor-pointer place-items-center rounded-md text-muted-foreground transition-colors hover:bg-[var(--color-accent)] hover:text-foreground"
      >
        <X className="size-3.5" />
      </button>

      <div className="px-5 py-5 sm:px-6 sm:py-6">
        <h2
          id="firstrun-heading"
          className="font-display text-[20px] font-bold tracking-tight text-foreground sm:text-[22px]"
        >
          Welcome to {tenantName}
        </h2>
        <p className="mt-1 max-w-xl text-[12.5px] leading-relaxed text-muted-foreground">
          Your tenant is provisioned and ready. Here's where most teams start.
        </p>

        <ul className="mt-5 grid grid-cols-1 gap-2.5 sm:grid-cols-2 xl:grid-cols-4">
          {SETUP_TILES.map((tile, idx) => (
            <li
              key={tile.to}
              className="fsh-enter"
              style={{ animationDelay: `${80 + idx * 60}ms` }}
            >
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
        "group/tile flex h-full flex-col gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-3.5",
        "transition-colors duration-200 hover:border-[var(--color-border-strong)] hover:bg-[var(--color-accent)]",
      )}
    >
      <div className="flex items-start justify-between">
        <span
          aria-hidden
          className={cn(
            "grid size-8 place-items-center rounded-md",
            STAT_TONE_BG[spec.tone],
          )}
        >
          <Icon className="size-3.5" />
        </span>
        <span className="text-[10.5px] font-medium uppercase tracking-wider text-muted-foreground">
          Step {spec.step}
        </span>
      </div>

      <div>
        <div className="text-[13px] font-semibold tracking-tight text-foreground">
          {spec.title}
        </div>
        <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">
          {spec.description}
        </p>
      </div>

      <div className="mt-auto flex items-center gap-1 pt-1 text-[11px] font-medium text-muted-foreground transition-colors group-hover/tile:text-foreground">
        Open
        <ArrowRight className="size-3 transition-transform group-hover/tile:translate-x-0.5" />
      </div>
    </Link>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

export function OverviewPage() {
  const { user } = useAuth();
  const { status: sseStatus, eventCount } = useSseStatus();
  const { events } = useSseEvents();

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

  // ── Header strings ────────────────────────────────────────────────────
  const now = new Date();
  const dateCaption = now.toLocaleDateString("en-US", {
    weekday: "long",
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
  const greeting = getGreeting();
  const firstName = (user?.name ?? user?.email?.split("@")[0] ?? "operator")
    .toString()
    .split(" ")[0];
  const tenantLabel = user?.tenant ?? "your tenant";

  // ── Stat values ───────────────────────────────────────────────────────
  const planValue = subscription.isLoading ? (
    <Skeleton className="h-5 w-16" />
  ) : (
    subscription.data?.planKey ?? "—"
  );
  const planSub = subscription.data
    ? subscription.data.status
    : "No subscription";

  const resourcesValue = usage.isLoading ? (
    <Skeleton className="h-5 w-10" />
  ) : (
    formatNumber(totalsView.resourceCount)
  );
  const resourcesSub = (
    <>
      <span className="tabular-nums">{totalsView.avgUtilization.toFixed(0)}%</span>{" "}
      avg utilization
      {totalsView.overage > 0 && (
        <>
          {" · "}
          <span className="text-[var(--color-destructive)]">
            {formatNumber(totalsView.overage)} overage
          </span>
        </>
      )}
    </>
  );

  return (
    <div className="space-y-5">
      {showFirstRun && (
        <FirstRunPanel
          tenantName={tenantLabel}
          tenantId={tenantId}
          onDismiss={() => setDismissed(true)}
        />
      )}

      {/* ── Editorial greeting header ───────────────────────────────────
          Direct text — no card chrome. Small caption above (date + tenant),
          big greeting below, action buttons on the right. */}
      <header className="fsh-enter flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div className="min-w-0">
          <p className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
            {dateCaption} · {tenantLabel}
          </p>
          <h1 className="mt-1 font-display text-display-page font-bold leading-tight tracking-tight text-foreground">
            {greeting}, {firstName}
          </h1>
        </div>
        <div className="flex shrink-0 flex-wrap items-center gap-2">
          <Button variant="outline" size="sm" disabled={refreshing} onClick={onRefresh}>
            <RefreshCw className={cn("mr-1.5 size-3.5", refreshing && "animate-spin")} />
            Refresh
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link to="/activity">
              <Activity className="mr-1.5 size-3.5" />
              View activity
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link to="/system/audits">
              <ScrollText className="mr-1.5 size-3.5" />
              View audits
            </Link>
          </Button>
        </div>
      </header>

      {/* ── Stats row — 4 cards ─────────────────────────────────────── */}
      <div className="grid grid-cols-2 gap-2.5 md:grid-cols-4">
        <StatCard
          index={0}
          tone="primary"
          icon={Server}
          label="Plan"
          value={planValue}
          sublabel={planSub}
        />
        <StatCard
          index={1}
          tone="success"
          icon={Calendar}
          label="Period"
          value={currentPeriodLabel()}
          sublabel={`${daysLeftInMonth()} days remaining`}
        />
        <StatCard
          index={2}
          tone="warning"
          icon={Gauge}
          label="Resources"
          value={resourcesValue}
          sublabel={resourcesSub}
        />
        <StatCard
          index={3}
          tone="info"
          icon={Zap}
          label="Live events"
          value={<span className="tabular-nums">{formatNumber(eventCount)}</span>}
          sublabel={
            <span className="inline-flex items-center gap-1.5">
              <span
                aria-hidden
                className={cn(
                  "inline-block size-1.5 rounded-full",
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
          href="/activity"
        />
      </div>

      {/* ── Multi-column widget grid ────────────────────────────────────
          Left rail (360px) holds the subscription summary and system
          status. The right side fills with a 2-up grid of secondary
          widgets that all read at the same density. */}
      <div className="flex flex-col gap-4 lg:flex-row">
        {/* Left rail */}
        <aside className="w-full space-y-4 lg:w-[360px] lg:shrink-0">
          <EntityDetailSection title="Subscription" icon={CreditCard}>
            <SubscriptionBody data={subscription.data} loading={subscription.isLoading} />
          </EntityDetailSection>

          <EntityDetailSection title="System status" icon={Wifi}>
            <SystemStatusBody sseStatus={sseStatus} eventCount={eventCount} />
          </EntityDetailSection>
        </aside>

        {/* Right column — 2-up widget grid */}
        <div className="grid w-full min-w-0 flex-1 grid-cols-1 gap-4 md:grid-cols-2">
          <EntityDetailSection
            title="Recent audits"
            icon={ScrollText}
            description="Last 24 hours, top 5 events."
            action={
              <Link
                to="/system/audits"
                className="inline-flex items-center gap-1 text-[11px] font-medium text-muted-foreground transition-colors hover:text-foreground"
              >
                See all <ArrowUpRight className="size-3" />
              </Link>
            }
          >
            <RecentAuditsBody />
          </EntityDetailSection>

          <EntityDetailSection
            title="Usage by resource"
            icon={Gauge}
            description="Current-month consumption against plan limits."
            action={
              totalsView.overage > 0 ? <Badge variant="danger">overage</Badge> : undefined
            }
          >
            {usage.isLoading ? (
              <UsageSkeleton />
            ) : usage.isError ? (
              <UsageEmpty
                title="Couldn't load usage"
                description="The usage endpoint returned an error. Try refreshing."
              />
            ) : rows.length === 0 ? (
              <UsageEmpty
                title="No usage captured yet"
                description="Activity will appear here as the backend records snapshots for this period."
              />
            ) : (
              <ul>
                {rows.map((row) => (
                  <UsageRow key={row.resource} row={row} />
                ))}
              </ul>
            )}
          </EntityDetailSection>

          <EntityDetailSection
            title="Quick actions"
            icon={Sparkles}
            description="Jump into the most-used destinations."
          >
            <QuickActionsBody />
          </EntityDetailSection>

          <EntityDetailSection
            title="Live feed"
            icon={Activity}
            description="Real-time backend events over SSE."
            action={
              <Link
                to="/activity"
                className="inline-flex items-center gap-1 text-[11px] font-medium text-muted-foreground transition-colors hover:text-foreground"
              >
                Open <ArrowUpRight className="size-3" />
              </Link>
            }
          >
            <LiveFeedBody events={events} />
          </EntityDetailSection>
        </div>
      </div>
    </div>
  );
}
