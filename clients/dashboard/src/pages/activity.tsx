import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Activity, Inbox } from "lucide-react";
import i18n from "@/i18n";
import { useSseEvents, useSseStatus, type SseEvent } from "@/sse/sse-context";
import { formatNumber } from "@/lib/list-helpers";
import { Badge } from "@/components/ui/badge";
import {
  EntityEmpty,
  EntityListCard,
  EntityListHeader,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
  type EntityStatusTone,
} from "@/components/list";

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

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

const DESKTOP_GRID = "grid-cols-[1fr_240px_120px]";

export function ActivityPage() {
  const { t } = useTranslation("activity");
  const { status, eventCount } = useSseStatus();
  const { events } = useSseEvents();

  const items = useMemo(() => events.slice(0, 200), [events]);
  const isLive = status === "connected";

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Activity}
        title={t("title")}
        total={eventCount}
        unit={t("unit")}
        description={t("description")}
      >
        {isLive ? (
          <Badge variant="success">{t("status.streaming")}</Badge>
        ) : status === "error" ? (
          <Badge variant="danger">{t("status.offline")}</Badge>
        ) : (
          <Badge variant="default">{t(`status.${status}`)}</Badge>
        )}
      </EntityPageHeader>

      {items.length === 0 ? (
        <EntityEmpty
          icon={Inbox}
          title={isLive ? t("empty.listeningTitle") : t("empty.noEventsTitle")}
          body={
            isLive
              ? t("empty.listeningBody")
              : t("empty.offlineBody")
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {t("shown", { count: items.length })}
              <span className="ml-2 opacity-60">
                · {t("totalCount", { total: formatNumber(eventCount) })}
              </span>
            </p>
          </div>

          {/* Mobile: card list */}
          <div
            className="space-y-2 md:hidden"
            role="log"
            aria-live="polite"
            aria-relevant="additions"
            aria-label={t("ariaLabel")}
          >
            {items.map((ev) => (
              <MobileCard key={ev.id} ev={ev} />
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
            <EntityListHeader className={DESKTOP_GRID}>
              <span>{t("col.action")}</span>
              <span>{t("col.entity")}</span>
              <span className="text-right">{t("col.time")}</span>
            </EntityListHeader>
            {items.map((ev, i) => (
              <DesktopRow
                key={ev.id}
                ev={ev}
                isLast={i === items.length - 1}
              />
            ))}
          </EntityListCard>
        </div>
      )}
    </div>
  );
}

// Mobile uses a static div (no navigation target — the activity feed is
// a stream of events, not a list of routable entities).
function MobileCard({ ev }: { ev: SseEvent }) {
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

function DesktopRow({ ev, isLast }: { ev: SseEvent; isLast: boolean }) {
  return (
    <EntityListRow className={DESKTOP_GRID} isLast={isLast}>
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
