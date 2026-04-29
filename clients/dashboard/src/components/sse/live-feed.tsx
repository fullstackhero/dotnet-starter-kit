import { useMemo } from "react";
import { Activity } from "lucide-react";
import { useSse, type SseEvent } from "@/sse/sse-context";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/cn";

const timeFmt = new Intl.DateTimeFormat("en-US", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});

function formatTime(ts: number) {
  return timeFmt.format(new Date(ts));
}

function formatPayload(data: unknown, raw: string): string {
  if (typeof data === "string") return data;
  try {
    return JSON.stringify(data, null, 2);
  } catch {
    return raw;
  }
}

/**
 * Heuristic for a status tone given the event type. Generic verbs map
 * to specific tones so the feed reads at-a-glance: failures pop red,
 * successes pop green, everything else stays neutral.
 */
function eventTone(type: string): "default" | "success" | "warning" | "danger" | "brand" {
  const t = type.toLowerCase();
  if (t.includes("fail") || t.includes("error") || t.includes("revoke")) return "danger";
  if (t.includes("warn") || t.includes("retry")) return "warning";
  if (t.includes("login") || t.includes("issued") || t.includes("created")) return "success";
  if (t.includes("token") || t.includes("auth")) return "brand";
  return "default";
}

function FeedRow({ ev }: { ev: SseEvent }) {
  const tone = useMemo(() => eventTone(ev.type), [ev.type]);
  return (
    <li
      className={cn(
        "fsh-enter group/row grid grid-cols-[auto_1fr] gap-x-4 gap-y-1",
        "border-t border-[var(--color-border)] px-6 py-3 first:border-t-0",
        "transition-colors hover:bg-[var(--color-muted)]",
      )}
    >
      <div className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatTime(ev.receivedAt)}
      </div>
      <div className="flex items-center gap-2">
        <Badge variant={tone}>{ev.type}</Badge>
      </div>
      <div className="col-start-2">
        <pre className="overflow-x-auto whitespace-pre-wrap break-words font-mono text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
          {formatPayload(ev.data, ev.rawData)}
        </pre>
      </div>
    </li>
  );
}

export function LiveFeed({ limit = 25 }: { limit?: number }) {
  const { events, status, eventCount } = useSse();
  const visible = useMemo(() => events.slice(0, limit), [events, limit]);
  const isLive = status === "connected";

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              Live activity
              {isLive ? (
                <Badge variant="success">streaming</Badge>
              ) : status === "error" ? (
                <Badge variant="danger">offline</Badge>
              ) : (
                <Badge variant="default">{status}</Badge>
              )}
            </CardTitle>
            <CardDescription>
              Real-time backend events delivered over Server-Sent Events.
            </CardDescription>
          </div>
          <div className="hidden items-center gap-2 sm:flex">
            <span className="font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
              total
            </span>
            <span className="font-mono text-sm tabular-nums">
              {new Intl.NumberFormat("en-US").format(eventCount)}
            </span>
          </div>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {visible.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-2 px-6 py-12 text-center">
            <span
              aria-hidden
              className="grid h-9 w-9 place-items-center rounded-full bg-[var(--color-muted)]"
            >
              <Activity className="h-4 w-4 text-[var(--color-muted-foreground)]" />
            </span>
            <div className="text-sm font-medium tracking-tight">
              {isLive ? "Listening for activity" : "No events yet"}
            </div>
            <p className="max-w-xs text-xs leading-relaxed text-[var(--color-muted-foreground)]">
              {isLive
                ? "The stream is open. Events will appear here as the backend publishes them."
                : "The activity stream is not connected. Events will queue once the connection comes online."}
            </p>
          </div>
        ) : (
          <ul>
            {visible.map((ev) => (
              <FeedRow key={ev.id} ev={ev} />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
