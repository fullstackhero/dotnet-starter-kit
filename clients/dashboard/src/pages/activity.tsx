import { useEffect, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Activity, Inbox, Loader2 } from "lucide-react";
import { useSseEvents, useSseStatus } from "@/sse/sse-context";
import { listAudits, auditPredicate, AUDIT_EVENT_TYPE_LABELS } from "@/api/audits";
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

const timeFmt = new Intl.DateTimeFormat("en-US", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});

function formatTime(ts: number) {
  return timeFmt.format(new Date(ts));
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

function eventTone(type: string): EntityStatusTone {
  const t = type.toLowerCase();
  if (t.includes("fail") || t.includes("error") || t.includes("revoke")) return "danger";
  if (t.includes("warn") || t.includes("retry")) return "warning";
  if (t.includes("login") || t.includes("issued") || t.includes("created")) return "success";
  if (t.includes("token") || t.includes("auth")) return "info";
  return "default";
}

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

export type DisplayActivityItem = {
  id: string;
  type: string;
  summary: string;
  entity: string;
  timestamp: number;
};

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

const DESKTOP_GRID = "grid-cols-[1fr_240px_120px]";

export function ActivityPage() {
  const queryClient = useQueryClient();
  const { status, eventCount } = useSseStatus();
  const { events } = useSseEvents();

  // TanStack Query: Fetch current accurate activity state from the backend API on mount/refresh
  const activityQuery = useQuery({
    queryKey: ["audits", "activity-feed"],
    queryFn: () => listAudits({ pageSize: 50 }),
    staleTime: 10_000,
  });

  // Real-time invalidation: when real-time SSE notifications arrive, invalidate the TanStack Query cache
  // so the UI automatically refetches fresh data from the backend API instead of stale local state.
  useEffect(() => {
    if (events.length > 0) {
      void queryClient.invalidateQueries({ queryKey: ["audits", "activity-feed"] });
    }
  }, [events, queryClient]);

  const items = useMemo<DisplayActivityItem[]>(() => {
    const sseItems: DisplayActivityItem[] = events.map((ev) => ({
      id: ev.id,
      type: ev.type,
      summary: payloadSummary(ev.data, ev.rawData),
      entity: entityLabel(ev.data),
      timestamp: ev.receivedAt,
    }));

    const apiItems: DisplayActivityItem[] = (activityQuery.data?.items ?? []).map((audit) => ({
      id: audit.id,
      type: AUDIT_EVENT_TYPE_LABELS[audit.eventType] ?? audit.eventType,
      summary: auditPredicate(audit),
      entity: audit.userName ?? audit.source ?? "—",
      timestamp: new Date(audit.occurredAtUtc).getTime(),
    }));

    // Merge API query results with live SSE items, deduping by id
    const seen = new Set<string>();
    const merged: DisplayActivityItem[] = [];

    for (const item of [...sseItems, ...apiItems]) {
      if (!seen.has(item.id)) {
        seen.add(item.id);
        merged.push(item);
      }
    }

    return merged.sort((a, b) => b.timestamp - a.timestamp).slice(0, 200);
  }, [events, activityQuery.data]);

  const isLive = status === "connected";

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Activity}
        title="Live activity"
        total={items.length || eventCount}
        unit="event"
        description="Activity log fetched via TanStack Query and updated in real time over SSE."
      >
        {isLive ? (
          <Badge variant="success">streaming</Badge>
        ) : status === "error" ? (
          <Badge variant="danger">offline</Badge>
        ) : (
          <Badge variant="default">{status}</Badge>
        )}
      </EntityPageHeader>

      {activityQuery.isLoading ? (
        <div className="flex items-center justify-center p-12 text-[var(--color-muted-foreground)]">
          <Loader2 className="mr-2 h-5 w-5 animate-spin" />
          <span>Loading activity log…</span>
        </div>
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={Inbox}
          title={isLive ? "Listening for activity" : "No events yet"}
          body={
            isLive
              ? "The stream is open. Events will appear here as the backend publishes them."
              : "The activity stream is not connected. Events will load once the connection comes online."
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {items.length} event{items.length === 1 ? "" : "s"} shown
            </p>
          </div>

          {/* Mobile: card list */}
          <div
            className="space-y-2 md:hidden"
            role="log"
            aria-live="polite"
            aria-relevant="additions"
            aria-label="Activity events"
          >
            {items.map((item) => (
              <MobileCard key={item.id} item={item} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard
            className="hidden md:block"
            role="log"
            aria-live="polite"
            aria-relevant="additions"
            aria-label="Activity events"
          >
            <EntityListHeader className={DESKTOP_GRID}>
              <span>Action</span>
              <span>Entity</span>
              <span className="text-right">Time</span>
            </EntityListHeader>
            {items.map((item, i) => (
              <DesktopRow
                key={item.id}
                item={item}
                isLast={i === items.length - 1}
              />
            ))}
          </EntityListCard>
        </div>
      )}
    </div>
  );
}

function MobileCard({ item }: { item: DisplayActivityItem }) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
      <div className="flex items-center justify-between gap-2">
        <EntityStatusBadge tone={eventTone(item.type)}>{item.type}</EntityStatusBadge>
        <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {formatTime(item.timestamp)}
        </span>
      </div>
      <p className="mt-2 line-clamp-2 break-words font-mono text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
        {item.summary}
      </p>
    </div>
  );
}

function DesktopRow({ item, isLast }: { item: DisplayActivityItem; isLast: boolean }) {
  return (
    <EntityListRow className={DESKTOP_GRID} isLast={isLast}>
      <div className="flex min-w-0 items-center gap-2">
        <EntityStatusBadge tone={eventTone(item.type)}>{item.type}</EntityStatusBadge>
        <span className="truncate font-mono text-[11.5px] text-[var(--color-muted-foreground)]">
          {item.summary}
        </span>
      </div>
      <code
        title={item.entity}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {item.entity}
      </code>
      <span className="text-right font-mono text-[11.5px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatTime(item.timestamp)}
      </span>
    </EntityListRow>
  );
}

