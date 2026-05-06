import { useSse } from "@/sse/sse-context";
import { cn } from "@/lib/cn";

const LABELS = {
  idle: "Idle",
  connecting: "Connecting…",
  connected: "Live",
  reconnecting: "Reconnecting…",
  error: "Offline",
} as const;

/**
 * SSE indicator pill. When connected the dot breathes via the
 * `pulse-dot` utility (currentColor-driven, so the glow inherits the
 * dot's status hue). All colors come from semantic tokens — no literals.
 */
export function SseStatusBadge() {
  const { status, eventCount } = useSse();
  const isLive = status === "connected";
  const isError = status === "error";

  const dotColor = isLive
    ? "var(--color-success)"
    : isError
      ? "var(--color-destructive)"
      : "var(--color-muted-foreground)";

  return (
    <div className="gradient-border inline-flex items-center gap-2 rounded-full bg-[var(--color-surface-2)] px-2.5 py-1 text-xs">
      <span
        aria-hidden
        className={cn("inline-flex h-2 w-2 rounded-full", isLive && "pulse-dot")}
        style={{ backgroundColor: dotColor, color: dotColor }}
      />
      <span className="font-medium tracking-tight">{LABELS[status]}</span>
      {isLive && (
        <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
          · {eventCount}
        </span>
      )}
    </div>
  );
}
