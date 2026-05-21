import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { MessageCircle } from "lucide-react";
import { listMyChannels } from "@/api/chat";
import { cn } from "@/lib/cn";

/**
 * Topbar pill that exposes the sum of unread chat messages across every
 * channel the caller belongs to. Sits next to the NotificationBell — the
 * bell is destructive-red for system notifications (mentions etc.), this
 * one is brand-primary for chat unreads, so they're visually distinct.
 *
 * Backed by the same ["chat", "my-channels"] query the rail uses, so
 * TanStack Query dedupes the fetch. The count stays live because:
 *   - ChatGlobalNotifier invalidates this key on each ChatMessageCreated
 *   - markChannelRead (chat-page) invalidates it when the user reads
 *   - the composer invalidates it when the user sends
 */
export function ChatUnreadBadge() {
  const navigate = useNavigate();
  const { data: channels } = useQuery({
    queryKey: ["chat", "my-channels"],
    queryFn: () => listMyChannels({ pageSize: 100 }),
    staleTime: 30_000,
  });

  const unread = useMemo(
    () => (channels ?? []).reduce((sum, c) => sum + (c.unreadCount ?? 0), 0),
    [channels],
  );

  return (
    <button
      type="button"
      aria-label={`Chat${unread > 0 ? `, ${unread} unread message${unread === 1 ? "" : "s"}` : ""}`}
      title={unread > 0 ? `${unread} unread chat message${unread === 1 ? "" : "s"}` : "Chat"}
      onClick={() => navigate("/chat")}
      className={cn(
        "relative grid h-9 w-9 cursor-pointer place-items-center rounded-md",
        "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
      )}
    >
      <MessageCircle className="h-4 w-4" aria-hidden />
      {unread > 0 && (
        <span
          aria-hidden
          className={cn(
            "absolute -right-0.5 -top-0.5 grid min-w-[16px] place-items-center rounded-full",
            "h-4 px-1 font-mono text-[9.5px] font-semibold tabular-nums leading-none",
            "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]",
            "ring-2 ring-[var(--color-background)]",
          )}
        >
          {unread > 99 ? "99+" : unread}
        </span>
      )}
    </button>
  );
}
