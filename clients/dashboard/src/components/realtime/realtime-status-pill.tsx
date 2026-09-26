import { useTranslation } from "react-i18next";
import { useRealtime } from "@/realtime/realtime-context";
import { cn } from "@/lib/cn";

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
  const { t } = useTranslation("common");
  const { status } = useRealtime();
  const labels: Record<string, string> = {
    idle: t("realtimeStatus.offline"),
    connecting: t("realtimeStatus.connecting"),
    connected: t("realtimeStatus.live"),
    reconnecting: t("realtimeStatus.reconnecting"),
    error: t("realtimeStatus.offline"),
  };
  const label = labels[status] ?? t("realtimeStatus.offline");
  return (
    <span
      className={cn("chat-status-pill", className)}
      data-status={status}
      {...(announce ? { role: "status", "aria-live": "polite" as const } : {})}
      title={t("realtimeStatus.title", { label })}
    >
      <span aria-hidden className="chat-status-dot" />
      <span>{label}</span>
    </span>
  );
}
