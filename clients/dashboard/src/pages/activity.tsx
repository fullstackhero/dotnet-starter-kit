import { useMemo } from "react";
import { Activity, Inbox } from "lucide-react";
import { useSseEvents, useSseStatus, type SseEvent } from "@/sse/sse-context";
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
  const { status, eventCount } = useSseStatus();
  const { events } = useSseEvents();

  const items = useMemo(() => events.slice(0, 200), [events]);
  const isLive = status === "connected";

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Activity}
        title="Live activity"
        total={eventCount}
        unit="event"
        description="Full event log streamed from the API over Server-Sent Events."
      >
        {isLive ? (
          <Badge variant="success">streaming</Badge>
        ) : status === "error" ? (
          <Badge variant="danger">offline</Badge>
        ) : (
          <Badge variant="default">{status}</Badge>
        )}
      </EntityPageHeader>

      {items.length === 0 ? (
        <EntityEmpty
          icon={Inbox}
          title={isLive ? "Listening for activity" : "No events yet"}
          body={
            isLive
              ? "The stream is open. Events will appear here as the backend publishes them."
              : "The activity stream is not connected. Events will queue once the connection comes online."
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {items.length} event{items.length === 1 ? "" : "s"} shown
              <span className="ml-2 opacity-60">
                · {new Intl.NumberFormat("en-US").format(eventCount)} total
              </span>
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
            aria-label="Activity events"
          >
            <EntityListHeader className={DESKTOP_GRID}>
              <span>Action</span>
              <span>Entity</span>
              <span className="text-right">Time</span>
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
