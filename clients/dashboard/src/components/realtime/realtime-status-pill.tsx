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
 */
export function RealtimeStatusPill({ className }: { className?: string }) {
  const { status } = useRealtime();
  const label = LABEL[status] ?? "Offline";
  return (
    <span
      className={cn("chat-status-pill", className)}
      data-status={status}
      role="status"
      aria-live="polite"
      title={`Realtime: ${label}`}
    >
      <span aria-hidden className="chat-status-dot" />
      <span>{label}</span>
    </span>
  );
}
