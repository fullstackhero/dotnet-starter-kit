import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
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
  getMyStatus,
  getMySubscription,
  getUsageSnapshots,
  type SubscriptionDto,
  type TenantExpiryState,
  type TenantStatusDto,
  type UsageSnapshotDto,
} from "@/api/billing";
import {
  AuditEventType,
  AuditSeverity,
  AUDIT_EVENT_TYPE_LABELS,
  severityRank,
  listAudits,
  type AuditSummaryDto,
} from "@/api/audits";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { EntityDetailSection } from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { useSseEvents, useSseStatus, type SseEvent, type SseStatus } from "@/sse/sse-context";
import i18n from "@/i18n";
import { formatDate, formatNumber } from "@/lib/list-helpers";
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

/**
 * Fraction of the subscription term elapsed, 0..1 — computed from the
 * subscription's own start→end window (NOT the calendar month), so it tracks
 * the same validity the operator sees. Returns null for an open-ended or
 * unparseable term (no finite window to chart). A future-dated start clamps
 * to 0, a past end clamps to 1.
 */
function subscriptionProgress(
  startUtc: string,
  endUtc: string | null | undefined,
  now: Date = new Date(),
): number | null {
  if (!endUtc) return null;
  const start = Date.parse(startUtc);
  const end = Date.parse(endUtc);
  if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) return null;
  return Math.min(1, Math.max(0, (now.getTime() - start) / (end - start)));
}

/** Whole days from now until `iso`, floored at 0. */
function daysUntil(iso: string, now: Date = new Date()): number {
  const target = Date.parse(iso);
  if (!Number.isFinite(target)) return 0;
  return Math.max(0, Math.ceil((target - now.getTime()) / 86_400_000));
}

/**
 * Time-of-day greeting. Three buckets — morning (<12), afternoon (<17),
 * evening (rest). Mirrors the dentalOS dashboard greeting helper so the
 * tone of voice matches across products.
 */
function greetingKey(): "greeting.morning" | "greeting.afternoon" | "greeting.evening" {
  const hour = new Date().getHours();
  if (hour < 12) return "greeting.morning";
  if (hour < 17) return "greeting.afternoon";
  return "greeting.evening";
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

/**
 * Derive the "Valid for" stat-card view from the tenant's expiry state — the
 * same source of truth the Subscription page's ValidityBody reads. Drives the
 * card tone (success/warning/danger) and the days-left target so an in-grace
 * or expired tenant sees the warning instead of a healthy number.
 */
function validityView(status: TenantStatusDto | undefined): {
  tone: StatTone;
  state: TenantExpiryState | undefined;
  /** ISO the card counts down to (validUpto when active, graceEnds when in grace). */
  targetUtc: string | null;
  daysLeft: number | null;
} {
  if (!status) {
    return { tone: "success", state: undefined, targetUtc: null, daysLeft: null };
  }
  if (status.expiryState === "Expired") {
    return { tone: "danger", state: "Expired", targetUtc: null, daysLeft: 0 };
  }
  if (status.expiryState === "InGrace") {
    const target = status.graceEndsUtc || null;
    return {
      tone: "warning",
      state: "InGrace",
      targetUtc: target,
      daysLeft: target ? daysUntil(target) : 0,
    };
  }
  // Active.
  const target = status.validUpto || null;
  return {
    tone: "success",
    state: "Active",
    targetUtc: target,
    daysLeft: target ? daysUntil(target) : null,
  };
}

function formatClock(ts: number) {
  // 24h hh:mm:ss in the active locale — no list-helper covers second precision.
  return new Intl.DateTimeFormat(i18n.language, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  }).format(new Date(ts));
}

function statusLabel(status: string, t: TFunction): string {
  return t(`stat.status.${status}`, { defaultValue: status });
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

type StatTone = "primary" | "success" | "warning" | "danger" | "info";

const STAT_TONE_BG: Record<StatTone, string> = {
  primary: "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]",
  success: "bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]",
  warning: "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.12)] text-[var(--color-warning)]",
  danger: "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] text-[var(--color-destructive)]",
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
  isError,
}: {
  data: SubscriptionDto | null | undefined;
  loading: boolean;
  isError: boolean;
}) {
  const { t } = useTranslation("overview");
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
  if (isError) {
    return (
      <div className="flex flex-col items-start gap-3">
        <div>
          <div className="text-[13px] font-semibold tracking-tight text-foreground">
            {t("subscription.loadErrorTitle")}
          </div>
          <p className="mt-1 text-[11.5px] leading-relaxed text-muted-foreground">
            {t("subscription.loadErrorBody")}
          </p>
        </div>
      </div>
    );
  }
  if (!data) {
    return (
      <div className="flex flex-col items-start gap-3">
        <div>
          <div className="text-[13px] font-semibold tracking-tight text-foreground">
            {t("subscription.noneTitle")}
          </div>
          <p className="mt-1 text-[11.5px] leading-relaxed text-muted-foreground">
            {t("subscription.noneBody")}
          </p>
        </div>
        <Button asChild variant="soft" size="sm">
          <Link to="/subscription">{t("subscription.viewCta")}</Link>
        </Button>
      </div>
    );
  }

  const progress = subscriptionProgress(data.startUtc, data.endUtc);
  const progressPct = progress === null ? null : Math.round(progress * 100);
  const daysLeft = data.endUtc ? daysUntil(data.endUtc) : null;

  return (
    <div className="space-y-4">
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-display text-[22px] font-bold tracking-tight text-foreground">
          {data.planKey}
        </span>
        {/* /subscriptions/me only ever returns the ACTIVE subscription, so
            the status badge is always the active tone. */}
        <Badge variant="success">
          <span
            aria-hidden
            className="pulse-dot inline-block h-1.5 w-1.5 rounded-full"
            style={{ backgroundColor: "var(--color-success)", color: "var(--color-success)" }}
          />
          {statusLabel(data.status, t)}
        </Badge>
      </div>

      <dl className="space-y-1.5 text-[12px]">
        <div className="flex items-center justify-between gap-3">
          <dt className="text-muted-foreground">{t("subscription.started")}</dt>
          <dd className="tabular-nums text-foreground">
            {formatDate(data.startUtc)}
          </dd>
        </div>
        <div className="flex items-center justify-between gap-3">
          <dt className="text-muted-foreground">{t("subscription.ends")}</dt>
          <dd className="tabular-nums text-foreground">
            {data.endUtc ? formatDate(data.endUtc) : t("subscription.openEnded")}
          </dd>
        </div>
      </dl>

      <div className="space-y-1.5">
        <div className="flex items-center justify-between text-[11px]">
          <span className="text-muted-foreground">{t("subscription.currentTerm")}</span>
          <span className="tabular-nums text-foreground">
            {progressPct === null
              ? t("subscription.openEnded")
              : t("subscription.termProgress", { pct: progressPct, days: daysLeft })}
          </span>
        </div>
        {progressPct !== null && (
          <div className="h-1 overflow-hidden rounded-full bg-[var(--color-muted)]">
            <div
              className="h-full rounded-full bg-[var(--color-primary)] transition-[width] duration-[700ms] ease-[var(--ease-out-cubic)]"
              style={{ width: `${progressPct}%` }}
            />
          </div>
        )}
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
  const { t } = useTranslation("overview");
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
              {live ? t("system.streamLive") : t(`connState.${sseStatus}`)}
            </span>
            {live && <Badge variant="success">SSE</Badge>}
            {errored && <Badge variant="danger">{t("system.offline")}</Badge>}
          </div>
          <p className="mt-0.5 text-[11.5px] text-muted-foreground">
            {live
              ? t("system.liveDesc")
              : errored
                ? t("system.erroredDesc")
                : t("system.waitingDesc")}
          </p>
        </div>
      </div>

      <div className="flex items-center justify-between border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] pt-3">
        <span className="text-[11px] uppercase tracking-wider text-muted-foreground">
          {t("system.eventsThisSession")}
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

function recentSeverityColor(severity: AuditSeverity): string {
  const rank = severityRank(severity);
  if (rank >= severityRank(AuditSeverity.Error)) return "var(--color-destructive)";
  if (rank >= severityRank(AuditSeverity.Warning)) return "var(--color-warning)";
  if (rank >= severityRank(AuditSeverity.Information)) return "var(--color-info)";
  return "var(--color-muted-foreground)";
}

function recentEventTypeIcon(eventType: AuditEventType): React.ComponentType<{ className?: string }> {
  if (eventType === AuditEventType.Security) return ShieldCheck;
  if (eventType === AuditEventType.Exception) return Activity;
  if (eventType === AuditEventType.EntityChange) return Server;
  return Activity;
}

function RecentAuditsBody() {
  const { t } = useTranslation("overview");
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
          {t("audits.emptyTitle")}
        </div>
        <p className="max-w-sm text-[11.5px] text-muted-foreground">
          {t("audits.emptyBody")}
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
  const { t } = useTranslation("overview");
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
            {row.source ?? AUDIT_EVENT_TYPE_LABELS[row.eventType] ?? t("audits.eventFallback")}
          </div>
          <div className="mt-0.5 flex items-center gap-1.5 text-[11px] text-muted-foreground">
            <span className="truncate">
              {row.userName ?? row.userId?.slice(0, 8) ?? t("audits.systemActor")}
            </span>
            <span aria-hidden>·</span>
            <span className="tabular-nums">{t("audits.ago", { value: relativeTime(row.occurredAtUtc) })}</span>
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
  titleKey: string;
  descriptionKey: string;
  icon: React.ComponentType<{ className?: string }>;
  tone: StatTone;
};

const QUICK_ACTIONS: QuickAction[] = [
  {
    to: "/identity/users",
    titleKey: "quick.inviteUsers.title",
    descriptionKey: "quick.inviteUsers.description",
    icon: UsersRound,
    tone: "info",
  },
  {
    to: "/catalog/products",
    titleKey: "quick.browseCatalog.title",
    descriptionKey: "quick.browseCatalog.description",
    icon: Package,
    tone: "success",
  },
  {
    to: "/subscription",
    titleKey: "quick.subscription.title",
    descriptionKey: "quick.subscription.description",
    icon: CreditCard,
    tone: "primary",
  },
  {
    to: "/activity",
    titleKey: "quick.liveActivity.title",
    descriptionKey: "quick.liveActivity.description",
    icon: Activity,
    tone: "warning",
  },
];

function QuickActionsBody() {
  const { t } = useTranslation("overview");
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
              {t(a.titleKey)}
            </div>
            <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">
              {t(a.descriptionKey)}
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
  const { t } = useTranslation("overview");
  const visible = useMemo(() => events.slice(0, 5), [events]);

  if (visible.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 py-6 text-center">
        <Activity className="size-4 text-muted-foreground" />
        <div className="text-[13px] font-semibold tracking-tight text-foreground">
          {t("liveFeed.emptyTitle")}
        </div>
        <p className="max-w-sm text-[11.5px] text-muted-foreground">
          {t("liveFeed.emptyBody")}
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
  titleKey: string;
  descriptionKey: string;
  icon: React.ComponentType<{ className?: string }>;
  tone: StatTone;
};

const SETUP_TILES: SetupTileSpec[] = [
  {
    to: "/invoices",
    step: "01",
    titleKey: "firstRun.tile.plan.title",
    descriptionKey: "firstRun.tile.plan.description",
    icon: Sparkles,
    tone: "primary",
  },
  {
    to: "/identity/users",
    step: "02",
    titleKey: "firstRun.tile.team.title",
    descriptionKey: "firstRun.tile.team.description",
    icon: UsersRound,
    tone: "info",
  },
  {
    to: "/catalog/products",
    step: "03",
    titleKey: "firstRun.tile.catalog.title",
    descriptionKey: "firstRun.tile.catalog.description",
    icon: Package,
    tone: "success",
  },
  {
    to: "/activity",
    step: "04",
    titleKey: "firstRun.tile.watch.title",
    descriptionKey: "firstRun.tile.watch.description",
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
  const { t } = useTranslation("overview");
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
        aria-label={t("firstRun.dismissAria")}
        title={t("firstRun.skipTitle")}
        className="absolute right-3 top-3 z-10 grid size-7 cursor-pointer place-items-center rounded-md text-muted-foreground transition-colors hover:bg-[var(--color-accent)] hover:text-foreground"
      >
        <X className="size-3.5" />
      </button>

      <div className="px-5 py-5 sm:px-6 sm:py-6">
        <h2
          id="firstrun-heading"
          className="font-display text-[20px] font-bold tracking-tight text-foreground sm:text-[22px]"
        >
          {t("firstRun.welcome", { name: tenantName })}
        </h2>
        <p className="mt-1 max-w-xl text-[12.5px] leading-relaxed text-muted-foreground">
          {t("firstRun.subtitle")}
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
  const { t } = useTranslation("overview");
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
          {t("firstRun.step", { step: spec.step })}
        </span>
      </div>

      <div>
        <div className="text-[13px] font-semibold tracking-tight text-foreground">
          {t(spec.titleKey)}
        </div>
        <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">
          {t(spec.descriptionKey)}
        </p>
      </div>

      <div className="mt-auto flex items-center gap-1 pt-1 text-[11px] font-medium text-muted-foreground transition-colors group-hover/tile:text-foreground">
        {t("open")}
        <ArrowRight className="size-3 transition-transform group-hover/tile:translate-x-0.5" />
      </div>
    </Link>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

export function OverviewPage() {
  const { t } = useTranslation("overview");
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

  // Tenant status drives the "Valid for" stat card and its tone — the same
  // source of truth (expiryState / validUpto / graceEndsUtc) the Subscription
  // page reads, so the landing page reflects grace/expired instead of a
  // healthy number computed from the subscription term alone.
  const status = useQuery({
    queryKey: ["tenant", "me", "status"],
    queryFn: () => getMyStatus(),
    staleTime: 60_000,
  });

  // First-run state — show only when the tenant has no active subscription
  // and the user hasn't dismissed it for this tenant. Gated on `!isError` so
  // an API failure surfaces an error branch instead of masquerading as a
  // first-run (no-plan) tenant. Re-checks on tenant change so switching
  // tenants restores the panel.
  const tenantId = user?.tenant;
  const [dismissed, setDismissed] = useState<boolean>(() => readDismissed(tenantId));
  useEffect(() => {
    setDismissed(readDismissed(tenantId));
  }, [tenantId]);
  const showFirstRun =
    !dismissed &&
    !subscription.isLoading &&
    !subscription.isError &&
    !subscription.data;

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

  const refreshing = usage.isFetching || subscription.isFetching || status.isFetching;
  const onRefresh = () => {
    void usage.refetch();
    void subscription.refetch();
    void status.refetch();
  };

  // ── Header strings ────────────────────────────────────────────────────
  const now = new Date();
  const dateCaption = now.toLocaleDateString(i18n.language, {
    weekday: "long",
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
  const firstName = (user?.name ?? user?.email?.split("@")[0] ?? t("operator"))
    .toString()
    .split(" ")[0];
  const tenantLabel = user?.tenant ?? t("yourTenant");

  // ── Stat values ───────────────────────────────────────────────────────
  const planValue = subscription.isLoading ? (
    <Skeleton className="h-5 w-16" />
  ) : (
    subscription.data?.planKey ?? "—"
  );
  const planSub = subscription.isError
    ? t("stat.unavailable")
    : subscription.data
      ? statusLabel(subscription.data.status, t)
      : t("stat.noSubscription");

  // ── Validity card — driven by the tenant's expiry state (validUpto /
  // graceEndsUtc), so an in-grace tenant counts down to grace-end and an
  // expired tenant reads "Expired" in a danger tone — matching the global
  // banner and the Subscription page. Falls back gracefully when status is
  // unavailable.
  const validity = useMemo(() => validityView(status.data), [status.data]);
  const validityValue = status.isLoading ? (
    <Skeleton className="h-5 w-16" />
  ) : status.isError || !status.data ? (
    "—"
  ) : validity.state === "Expired" ? (
    <span className="text-[var(--color-destructive)]">{t("validity.expired")}</span>
  ) : validity.daysLeft === null ? (
    t("validity.openEnded")
  ) : (
    <span className="inline-flex items-baseline gap-1">
      <span
        className={cn(
          "tabular-nums",
          validity.tone === "warning" && "text-[var(--color-warning)]",
        )}
      >
        {formatNumber(validity.daysLeft)}
      </span>
      <span className="text-[12px] font-medium text-muted-foreground">{t("validity.days")}</span>
    </span>
  );
  const validitySub = status.isLoading
    ? undefined
    : status.isError || !status.data
      ? t("validity.statusUnavailable")
      : validity.state === "Expired"
        ? t("validity.renew")
        : validity.state === "InGrace"
          ? validity.targetUtc
            ? t("validity.graceEnds", { date: formatDate(validity.targetUtc) })
            : t("validity.inGracePeriod")
          : validity.targetUtc
            ? t("validity.until", { date: formatDate(validity.targetUtc) })
            : t("validity.noEndDate");

  const resourcesValue = usage.isLoading ? (
    <Skeleton className="h-5 w-10" />
  ) : (
    formatNumber(totalsView.resourceCount)
  );
  const resourcesSub = (
    <>
      <span className="tabular-nums">{totalsView.avgUtilization.toFixed(0)}%</span>{" "}
      {t("resources.avgUtilization")}
      {totalsView.overage > 0 && (
        <>
          {" · "}
          <span className="text-[var(--color-destructive)]">
            {formatNumber(totalsView.overage)} {t("resources.overage")}
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
            {t(greetingKey(), { name: firstName })}
          </h1>
        </div>
        <div className="flex shrink-0 flex-wrap items-center gap-2">
          <Button variant="outline" size="sm" disabled={refreshing} onClick={onRefresh}>
            <RefreshCw className={cn("mr-1.5 size-3.5", refreshing && "animate-spin")} />
            {t("refresh")}
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link to="/activity">
              <Activity className="mr-1.5 size-3.5" />
              {t("viewActivity")}
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link to="/system/audits">
              <ScrollText className="mr-1.5 size-3.5" />
              {t("viewAudits")}
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
          label={t("stat.plan")}
          value={planValue}
          sublabel={planSub}
        />
        <StatCard
          index={1}
          tone={status.isLoading || status.isError || !status.data ? "success" : validity.tone}
          icon={Calendar}
          label={t("stat.validFor")}
          value={validityValue}
          sublabel={validitySub}
        />
        <StatCard
          index={2}
          tone="warning"
          icon={Gauge}
          label={t("stat.resources")}
          value={resourcesValue}
          sublabel={resourcesSub}
        />
        <StatCard
          index={3}
          tone="info"
          icon={Zap}
          label={t("stat.liveEvents")}
          value={<span className="tabular-nums">{formatNumber(eventCount)}</span>}
          sublabel={
            <span className="inline-flex items-center gap-1.5">
              <span
                aria-hidden
                className={cn(
                  "inline-block size-1.5 shrink-0 rounded-full",
                  // Contained opacity pulse — the design-system .pulse-dot halo
                  // (an overflowing ::before ring) gets clipped by this sublabel's
                  // `truncate` (overflow-hidden), so it can't be used here.
                  sseStatus === "connected" && "animate-pulse",
                )}
                style={{
                  backgroundColor:
                    sseStatus === "connected"
                      ? "var(--color-success)"
                      : sseStatus === "error"
                        ? "var(--color-destructive)"
                        : "var(--color-muted-foreground)",
                }}
              />
              <span className="capitalize">{t(`connState.${sseStatus}`)}</span>
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
          <EntityDetailSection title={t("section.subscription")} icon={CreditCard}>
            <SubscriptionBody
              data={subscription.data}
              loading={subscription.isLoading}
              isError={subscription.isError}
            />
          </EntityDetailSection>

          <EntityDetailSection title={t("section.systemStatus")} icon={Wifi}>
            <SystemStatusBody sseStatus={sseStatus} eventCount={eventCount} />
          </EntityDetailSection>
        </aside>

        {/* Right column — 2-up widget grid */}
        <div className="grid w-full min-w-0 flex-1 grid-cols-1 gap-4 md:grid-cols-2">
          <EntityDetailSection
            title={t("section.recentAudits")}
            icon={ScrollText}
            description={t("section.recentAuditsDesc")}
            action={
              <Link
                to="/system/audits"
                className="inline-flex items-center gap-1 text-[11px] font-medium text-muted-foreground transition-colors hover:text-foreground"
              >
                {t("seeAll")} <ArrowUpRight className="size-3" />
              </Link>
            }
          >
            <RecentAuditsBody />
          </EntityDetailSection>

          <EntityDetailSection
            title={t("section.usage")}
            icon={Gauge}
            description={t("section.usageDesc")}
            action={
              totalsView.overage > 0 ? <Badge variant="danger">{t("usageOverageBadge")}</Badge> : undefined
            }
          >
            {usage.isLoading ? (
              <UsageSkeleton />
            ) : usage.isError ? (
              <UsageEmpty
                title={t("usage.errorTitle")}
                description={t("usage.errorBody")}
              />
            ) : rows.length === 0 ? (
              <UsageEmpty
                title={t("usage.emptyTitle")}
                description={t("usage.emptyBody")}
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
            title={t("section.quickActions")}
            icon={Sparkles}
            description={t("section.quickActionsDesc")}
          >
            <QuickActionsBody />
          </EntityDetailSection>

          <EntityDetailSection
            title={t("section.liveFeed")}
            icon={Activity}
            description={t("section.liveFeedDesc")}
            action={
              <Link
                to="/activity"
                className="inline-flex items-center gap-1 text-[11px] font-medium text-muted-foreground transition-colors hover:text-foreground"
              >
                {t("open")} <ArrowUpRight className="size-3" />
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
