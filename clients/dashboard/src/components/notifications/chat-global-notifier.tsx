import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Hash, Lock, MessageCircle, Users2, X } from "lucide-react";
import type { ChannelDto, MessageDto } from "@/api/chat";
import { useAuth } from "@/auth/use-auth";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { useUserDisplay } from "@/lib/use-user-display";
import { cn } from "@/lib/cn";

/**
 * Background listener mounted in AppShell — subscribes to ChatMessageCreated
 * over SignalR and emits a sonner toast whenever a new message lands in a
 * channel the user is NOT currently viewing. Skips messages authored by self.
 *
 * Also invalidates the my-channels cache so the topbar unread badge + the
 * channel rail counters update immediately, even when the user is on a
 * page that doesn't otherwise touch chat queries.
 *
 * Renders nothing — pure side-effect component.
 */
export function ChatGlobalNotifier() {
  const { user } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // Track the currently-viewed channel via a ref so the SignalR handler
  // always reads the latest path without re-binding on every navigation.
  const currentChannelIdRef = useRef<string | null>(null);
  useEffect(() => {
    const m = location.pathname.match(/^\/chat\/([^/]+)/);
    currentChannelIdRef.current = m ? m[1] : null;
  }, [location.pathname]);

  useRealtimeEvent<MessageDto>(
    "ChatMessageCreated",
    (payload) => {
      if (!user?.id) return;
      if (payload.authorUserId === user.id) return;
      // Suppress when the user is already looking at this channel — they'll
      // see the message arrive in the live stream.
      if (payload.channelId === currentChannelIdRef.current) return;

      // Bump the badge + rail counters everywhere. The chat surface itself
      // patches its own cache from MessageList's listener; this is for
      // non-/chat pages.
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });

      toast.custom(
        (id) => (
          <ChatToast
            payload={payload}
            onView={() => {
              navigate(`/chat/${payload.channelId}`);
              toast.dismiss(id);
            }}
            onDismiss={() => toast.dismiss(id)}
          />
        ),
        { duration: 6_000 },
      );
    },
    [user?.id],
  );

  return null;
}

function ChatToast({
  payload,
  onView,
  onDismiss,
}: {
  payload: MessageDto;
  onView: () => void;
  onDismiss: () => void;
}) {
  const queryClient = useQueryClient();
  const author = useUserDisplay(payload.authorUserId);
  // Read the channel from cache rather than firing a fresh fetch — the
  // my-channels query was just invalidated by the notifier, so by the time
  // the toast paints the channel will usually be present (or shortly will be).
  const channels = queryClient.getQueryData<ChannelDto[]>(["chat", "my-channels"]);
  const channel = channels?.find((c) => c.id === payload.channelId);

  const channelLabel = !channel
    ? "a channel"
    : channel.type === 2
      ? `#${channel.name ?? "channel"}`
      : channel.type === 0
        ? "direct message"
        : "group chat";
  const Icon = !channel
    ? MessageCircle
    : channel.type === 2
      ? channel.isPrivate
        ? Lock
        : Hash
      : Users2;

  const body = (payload.body ?? "").trim();
  const preview = body.length > 120 ? `${body.slice(0, 120)}…` : body;

  return (
    <div
      className={cn(
        "relative flex w-[360px] items-start gap-3 rounded-xl border p-3 pr-9 text-left",
        "border-[var(--color-border)] bg-[var(--color-surface-1)] shadow-[var(--shadow-lift)]",
        // Subtle brand glow along the leading edge so the toast reads as a
        // chat affordance, not a system notice.
        "before:pointer-events-none before:absolute before:inset-y-2 before:left-0 before:w-0.5",
        "before:rounded-r-full before:bg-[var(--color-primary)] before:opacity-70",
      )}
      role="status"
      aria-live="polite"
    >
      <button
        type="button"
        onClick={onView}
        className={cn(
          "flex flex-1 cursor-pointer items-start gap-3 text-left",
          "rounded-md outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        )}
      >
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
        >
          <Icon className="h-3.5 w-3.5" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline gap-1.5">
            <span className="truncate text-sm font-semibold tracking-tight text-[var(--color-foreground)]">
              {author.name}
            </span>
            <span className="truncate font-mono text-[10.5px] uppercase tracking-[0.10em] text-[var(--color-muted-foreground)]">
              in {channelLabel}
            </span>
          </div>
          {preview ? (
            <p className="mt-0.5 line-clamp-2 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
              {preview}
            </p>
          ) : (
            <p className="mt-0.5 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
              (attachment or empty body)
            </p>
          )}
        </div>
      </button>

      <button
        type="button"
        onClick={(e) => {
          e.stopPropagation();
          onDismiss();
        }}
        aria-label="Dismiss"
        title="Dismiss"
        className={cn(
          "absolute right-2 top-2 grid h-6 w-6 cursor-pointer place-items-center rounded-md",
          "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        )}
      >
        <X className="h-3 w-3" aria-hidden />
      </button>
    </div>
  );
}
