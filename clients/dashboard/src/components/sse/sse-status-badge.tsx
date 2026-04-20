import { useSse } from "@/sse/sse-context";
import { cn } from "@/lib/cn";

const LABELS = {
  idle: "Idle",
  connecting: "Connecting…",
  connected: "Live",
  reconnecting: "Reconnecting…",
  error: "Offline",
} as const;

export function SseStatusBadge() {
  const { status, eventCount } = useSse();
  const isLive = status === "connected";

  return (
    <div className="inline-flex items-center gap-2 rounded-full border border-[var(--color-border)] bg-[var(--color-card)] px-2.5 py-1 text-xs">
      <span className="relative flex h-2 w-2">
        {isLive && (
          <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-500 opacity-75" />
        )}
        <span
          className={cn(
            "relative inline-flex h-2 w-2 rounded-full",
            isLive
              ? "bg-emerald-500"
              : status === "error"
                ? "bg-[var(--color-destructive)]"
                : "bg-[var(--color-muted-foreground)]",
          )}
        />
      </span>
      <span className="font-medium">{LABELS[status]}</span>
      {isLive && (
        <span className="text-[var(--color-muted-foreground)]">· {eventCount}</span>
      )}
    </div>
  );
}
