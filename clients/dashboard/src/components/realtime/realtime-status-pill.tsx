import { useRealtime } from "@/realtime/realtime-context";
import { cn } from "@/lib/cn";

const LABEL: Record<string, string> = {
  idle: "Offline",
  connecting: "Connecting",
  connected: "Live",
  reconnecting: "Reconnecting",
  error: "Offline",
};

/**
 * Compact connection-state indicator backed by the shared SignalR hub. Mono
 * caption + colored dot — green when live, amber pulsing while reconnecting,
 * destructive when down. Mounted in the chat rail footer and the notification
 * bell footer so the surfaces that depend on realtime stay honest about it.
 *
 * `announce` opt-in: only one instance of this pill should announce status
 * changes to AT (otherwise reconnect events get spoken twice). Pass
 * `announce={true}` on the canonical mount; leave it off elsewhere.
 */
export function RealtimeStatusPill({
  className,
  announce = false,
}: {
  className?: string;
  announce?: boolean;
}) {
  const { status } = useRealtime();
  const label = LABEL[status] ?? "Offline";
  return (
    <span
      className={cn("chat-status-pill", className)}
      data-status={status}
      {...(announce ? { role: "status", "aria-live": "polite" as const } : {})}
      title={`Realtime: ${label}`}
    >
      <span aria-hidden className="chat-status-dot" />
      <span>{label}</span>
    </span>
  );
}
