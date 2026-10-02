import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Activity, History, Inbox, Radio, RefreshCw, ScrollText, type LucideIcon } from "lucide-react";
import i18n from "@/i18n";
import { useSseEvents, useSseStatus, type SseEvent } from "@/sse/sse-context";
import { useAuth } from "@/auth/use-auth";
import {
  AuditEventType,
  AuditSeverity,
  auditPredicate,
  listAudits,
  severityRank,
  type AuditSummaryDto,
} from "@/api/audits";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  EntityEmpty,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
  ErrorBand,
  type EntityStatusTone,
} from "@/components/list";
import { describe, formatNumber, formatRelative } from "@/lib/list-helpers";

// Live SSE events exist only in memory (SseProvider) — by design, so nothing
// from one session can leak into the next after a logout or tenant switch.
// What survives a refresh is the persisted audit trail, so users who may read
// it get recent history loaded once from GET /audits. The two lists are
// rendered as separate sections, never merged: SSE events and audit records
// have unrelated ids and shapes, so any merge would duplicate or mis-order.

/** Mirrors the permission GET /api/v1/audits enforces server-side. */
const AUDIT_VIEW_PERMISSION = "Permissions.AuditTrails.View";
const HISTORY_PAGE_SIZE = 25;
const MAX_LIVE_EVENTS = 200;

// Audit enum labels resolve against the "audits" ns, same keys as the Audit trail page.
const EVENT_TYPE_KEY: Record<AuditEventType, string> = {
  None: "audits:eventType.none",
  EntityChange: "audits:eventType.entity",
  Security: "audits:eventType.security",
  Activity: "audits:eventType.activity",
  Exception: "audits:eventType.exception",
};
const SEVERITY_KEY: Record<AuditSeverity, string> = {
  None: "audits:severity.none",
  Trace: "audits:severity.trace",
  Debug: "audits:severity.debug",
  Information: "audits:severity.info",
  Warning: "audits:severity.warn",
  Error: "audits:severity.error",
  Critical: "audits:severity.critical",
};

function formatTime(ts: number) {
  // 24h hh:mm:ss in the active locale — no list-helper covers second precision.
  return new Intl.DateTimeFormat(i18n.language, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  }).format(new Date(ts));
}

function payloadSummary(data: unknown, raw: string): string {
  if (typeof data === "string") return data;
  if (data && typeof data === "object") {
    try {
      return JSON.stringify(data);
    } catch {
      return raw;
    }
  }
  return raw;
}

// Map an event type to a status badge tone — failures pop red, successes
// green, warnings amber, everything else neutral. Mirrors the heuristic
// the legacy live-feed component used.
function eventTone(type: string): EntityStatusTone {
  const t = type.toLowerCase();
  if (t.includes("fail") || t.includes("error") || t.includes("revoke")) return "danger";
  if (t.includes("warn") || t.includes("retry")) return "warning";
  if (t.includes("login") || t.includes("issued") || t.includes("created")) return "success";
  if (t.includes("token") || t.includes("auth")) return "info";
  return "default";
}

// Try to extract a friendlier "entity" label from the event payload —
// most domain events carry an aggregate id under a predictable field.
function entityLabel(data: unknown): string {
  if (data && typeof data === "object") {
    const obj = data as Record<string, unknown>;
    for (const key of ["entityId", "aggregateId", "id", "tenantId", "userId"]) {
      const v = obj[key];
      if (typeof v === "string" && v.length > 0) return v;
    }
  }
  return "—";
}

function severityTone(severity: AuditSeverity): EntityStatusTone {
  const rank = severityRank(severity);
  if (rank >= severityRank(AuditSeverity.Error)) return "danger";
  if (rank >= severityRank(AuditSeverity.Warning)) return "warning";
  if (rank >= severityRank(AuditSeverity.Information)) return "info";
  return "default";
}

function auditActor(row: AuditSummaryDto): string {
  return (
    row.userName ??
    (row.userId ? `${row.userId.slice(0, 8)}…` : i18n.t("actor.system", { ns: "audits" }))
  );
}

function formatAuditTime(iso: string) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "—";
  return new Intl.DateTimeFormat(i18n.language, {
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(d);
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

const LIVE_GRID = "grid-cols-[1fr_240px_120px]";
const HISTORY_GRID = "grid-cols-[1fr_200px_140px]";

export function ActivityPage() {
  const { t } = useTranslation("activity");
  const { status, eventCount } = useSseStatus();
  const { events } = useSseEvents();
  const { user, permissionsHydrated } = useAuth();

  const canViewHistory =
    permissionsHydrated && (user?.permissions.includes(AUDIT_VIEW_PERMISSION) ?? false);

  const liveItems = useMemo(() => events.slice(0, MAX_LIVE_EVENTS), [events]);
  const isLive = status === "connected";

  return (
    <div className="space-y-6 sm:space-y-8">
      <EntityPageHeader
        icon={Activity}
        title={t("title")}
        total={eventCount}
        unit="event"
        description={canViewHistory ? t("descriptionWithHistory") : t("descriptionLiveOnly")}
      >
        {isLive ? (
          <Badge variant="success">{t("status.streaming")}</Badge>
        ) : status === "error" ? (
          <Badge variant="danger">{t("status.offline")}</Badge>
        ) : (
          <Badge variant="default">{t(`status.${status}`)}</Badge>
        )}
      </EntityPageHeader>

      <LiveSection items={liveItems} isLive={isLive} eventCount={eventCount} />

      {!permissionsHydrated ? (
        <EntityListLoading rows={3} desktopColumns={HISTORY_GRID} />
      ) : canViewHistory ? (
        <HistorySection />
      ) : (
        <LiveOnlyNotice />
      )}
    </div>
  );
}

function SectionHeading({
  id,
  icon: Icon,
  title,
  caption,
  children,
}: {
  id: string;
  icon: LucideIcon;
  title: string;
  caption: string;
  children?: React.ReactNode;
}) {
  return (
    <div className="mb-3 flex flex-wrap items-end justify-between gap-2">
      <div className="min-w-0">
        <h2 id={id} className="flex items-center gap-2 font-display text-[15px] font-semibold tracking-tight text-[var(--color-foreground)]">
          <Icon className="size-4 text-[var(--color-muted-foreground)]" />
          {title}
        </h2>
        <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">{caption}</p>
      </div>
      {children && <div className="flex items-center gap-2">{children}</div>}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Live — SSE events received since this tab connected. In-memory only.
// ───────────────────────────────────────────────────────────────────────

function LiveSection({
  items,
  isLive,
  eventCount,
}: {
  items: SseEvent[];
  isLive: boolean;
  eventCount: number;
}) {
  const { t } = useTranslation("activity");
  return (
    <section aria-labelledby="activity-live-heading">
      <SectionHeading
        id="activity-live-heading"
        icon={Radio}
        title={t("live.title")}
        caption={t("live.caption")}
      >
        {items.length > 0 && (
          <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
            {t("shown", { count: items.length })}
            <span className="ml-2 opacity-60">
              · {t("totalCount", { total: formatNumber(eventCount) })}
            </span>
          </p>
        )}
      </SectionHeading>

      {items.length === 0 ? (
        <EntityListCard>
          <div className="flex items-center gap-3 px-5 py-4">
            <Inbox className="size-4 shrink-0 text-[var(--color-muted-foreground)]" />
            <div className="min-w-0">
              <p className="text-[13px] font-semibold text-[var(--color-foreground)]">
                {isLive ? t("empty.listeningTitle") : t("empty.noEventsTitle")}
              </p>
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                {isLive ? t("empty.listeningBody") : t("empty.offlineBody")}
              </p>
            </div>
          </div>
        </EntityListCard>
      ) : (
        <>
          {/* Mobile: card list */}
          <div
            className="space-y-2 md:hidden"
            role="log"
            aria-live="polite"
            aria-relevant="additions"
            aria-label={t("ariaLabel")}
          >
            {items.map((ev) => (
              <LiveMobileCard key={ev.id} ev={ev} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard
            className="hidden md:block"
            role="log"
            aria-live="polite"
            aria-relevant="additions"
            aria-label={t("ariaLabel")}
          >
            <EntityListHeader className={LIVE_GRID}>
              <span>{t("col.event")}</span>
              <span>{t("col.entity")}</span>
              <span className="text-right">{t("col.time")}</span>
            </EntityListHeader>
            {items.map((ev, i) => (
              <LiveDesktopRow key={ev.id} ev={ev} isLast={i === items.length - 1} />
            ))}
          </EntityListCard>
        </>
      )}
    </section>
  );
}

// Mobile uses a static div (no navigation target — the activity feed is
// a stream of events, not a list of routable entities).
function LiveMobileCard({ ev }: { ev: SseEvent }) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
      <div className="flex items-center justify-between gap-2">
        <EntityStatusBadge tone={eventTone(ev.type)}>{ev.type}</EntityStatusBadge>
        <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {formatTime(ev.receivedAt)}
        </span>
      </div>
      <p className="mt-2 line-clamp-2 break-words font-mono text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
        {payloadSummary(ev.data, ev.rawData)}
      </p>
    </div>
  );
}

function LiveDesktopRow({ ev, isLast }: { ev: SseEvent; isLast: boolean }) {
  return (
    <EntityListRow className={LIVE_GRID} isLast={isLast}>
      <div className="flex min-w-0 items-center gap-2">
        <EntityStatusBadge tone={eventTone(ev.type)}>{ev.type}</EntityStatusBadge>
        <span className="truncate font-mono text-[11.5px] text-[var(--color-muted-foreground)]">
          {payloadSummary(ev.data, ev.rawData)}
        </span>
      </div>
      <code
        title={entityLabel(ev.data)}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {entityLabel(ev.data)}
      </code>
      <span className="text-right font-mono text-[11.5px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatTime(ev.receivedAt)}
      </span>
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  History — the persisted audit trail, loaded once per visit (plus a
//  manual refresh). Deliberately NOT refetched per SSE event: a busy
//  tenant would otherwise hammer GET /audits.
// ───────────────────────────────────────────────────────────────────────

function HistorySection() {
  const { t } = useTranslation(["activity", "audits"]);
  const history = useQuery({
    queryKey: ["audits", "activity-history"],
    queryFn: ({ signal }) =>
      listAudits(
        {
          pageNumber: 1,
          pageSize: HISTORY_PAGE_SIZE,
          // Same default as the Audit trail page: drop the per-request system
          // Activity firehose so the history reads as meaningful events.
          excludeEventType: AuditEventType.Activity,
        },
        signal,
      ),
    staleTime: 60_000,
  });

  const rows = history.data?.items ?? [];

  return (
    <section aria-labelledby="activity-history-heading">
      <SectionHeading
        id="activity-history-heading"
        icon={History}
        title={t("history.title")}
        caption={t("history.caption", { size: HISTORY_PAGE_SIZE })}
      >
        <Button
          variant="outline"
          size="sm"
          onClick={() => void history.refetch()}
          disabled={history.isFetching}
          aria-label={t("history.refreshAria")}
        >
          <RefreshCw className={history.isFetching ? "animate-spin" : undefined} />
          {t("history.refresh")}
        </Button>
        <Button asChild variant="ghost" size="sm">
          <Link to="/system/audits">
            <ScrollText />
            {t("history.auditTrail")}
          </Link>
        </Button>
      </SectionHeading>

      {history.isError && (
        <div className="mb-3">
          <ErrorBand message={describe(history.error)} />
        </div>
      )}

      {history.isPending ? (
        <EntityListLoading rows={5} desktopColumns={HISTORY_GRID} />
      ) : rows.length === 0 ? (
        !history.isError && (
          <EntityEmpty
            icon={ScrollText}
            title={t("history.emptyTitle")}
            body={t("history.emptyBody")}
          />
        )
      ) : (
        <>
          <div className="space-y-2 md:hidden" aria-label={t("history.title")}>
            {rows.map((row) => (
              <HistoryMobileCard key={row.id} row={row} />
            ))}
          </div>

          <EntityListCard className="hidden md:block" aria-label={t("history.title")}>
            <EntityListHeader className={HISTORY_GRID}>
              <span>{t("col.event")}</span>
              <span>{t("col.severity")}</span>
              <span className="text-right">{t("col.time")}</span>
            </EntityListHeader>
            {rows.map((row, i) => (
              <HistoryDesktopRow key={row.id} row={row} isLast={i === rows.length - 1} />
            ))}
          </EntityListCard>
        </>
      )}
    </section>
  );
}

function HistoryMobileCard({ row }: { row: AuditSummaryDto }) {
  const { t } = useTranslation(["activity", "audits"]);
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
      <div className="flex items-center justify-between gap-2">
        <EntityStatusBadge tone={severityTone(row.severity)}>
          {EVENT_TYPE_KEY[row.eventType] ? t(EVENT_TYPE_KEY[row.eventType]) : row.eventType}
        </EntityStatusBadge>
        <span
          title={formatAuditTime(row.occurredAtUtc)}
          className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]"
        >
          {formatRelative(row.occurredAtUtc) || formatAuditTime(row.occurredAtUtc)}
        </span>
      </div>
      <p className="mt-2 text-[13px] text-[var(--color-foreground)]">
        <span className="font-semibold">{auditActor(row)}</span> {auditPredicate(row)}
      </p>
    </div>
  );
}

function HistoryDesktopRow({ row, isLast }: { row: AuditSummaryDto; isLast: boolean }) {
  const { t } = useTranslation(["activity", "audits"]);
  return (
    <EntityListRow className={HISTORY_GRID} isLast={isLast}>
      <div className="flex min-w-0 items-center gap-2">
        <EntityStatusBadge tone={severityTone(row.severity)}>
          {EVENT_TYPE_KEY[row.eventType] ? t(EVENT_TYPE_KEY[row.eventType]) : row.eventType}
        </EntityStatusBadge>
        <p className="truncate text-[13px] text-[var(--color-foreground)]">
          <span className="font-semibold">{auditActor(row)}</span> {auditPredicate(row)}
        </p>
      </div>
      <span className="text-[12px] text-[var(--color-muted-foreground)]">
        {SEVERITY_KEY[row.severity] ? t(SEVERITY_KEY[row.severity]) : row.severity}
      </span>
      <span
        title={formatAuditTime(row.occurredAtUtc)}
        className="text-right font-mono text-[11.5px] tabular-nums text-[var(--color-muted-foreground)]"
      >
        {formatAuditTime(row.occurredAtUtc)}
      </span>
    </EntityListRow>
  );
}

// Users without Audit trail access never call GET /audits (it would 403).
// Be upfront that the page only shows what arrives while it's open.
function LiveOnlyNotice() {
  const { t } = useTranslation("activity");
  return (
    <div
      role="note"
      className="flex items-start gap-3 rounded-xl border border-dashed border-[var(--color-border)] px-5 py-4"
    >
      <History className="mt-0.5 size-4 shrink-0 text-[var(--color-muted-foreground)]" />
      <div className="min-w-0">
        <p className="text-[13px] font-semibold text-[var(--color-foreground)]">
          {t("liveOnly.title")}
        </p>
        <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
          {t("liveOnly.body")}
        </p>
      </div>
    </div>
  );
}
