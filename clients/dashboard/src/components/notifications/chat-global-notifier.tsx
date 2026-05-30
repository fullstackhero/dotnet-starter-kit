import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Hash, Lock, MessageCircle, Users2, X } from "lucide-react";
import { ChannelType, type ChannelDto, type MessageDto } from "@/api/chat";
import { useAuth } from "@/auth/use-auth";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { useUserDisplay } from "@/lib/use-user-display";
import { Avatar } from "@/components/ui/avatar";
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

      // `className` is APPENDED to the toaster's global classNames.toast
      // ("fsh-toast"), so the wrapper li ends up with both .fsh-toast and
      // .fsh-chat-toast-wrapper. globals.css uses that compound selector
      // to neutralise the tone-rail chrome (bg, drain bar, "note" pseudo)
      // without touching the children sonner actually renders for our JSX.
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
        { duration: 6_000, className: "fsh-chat-toast-wrapper" },
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
    : channel.type === ChannelType.Channel
      ? `#${channel.name ?? "channel"}`
      : channel.type === ChannelType.DirectMessage
        ? "direct message"
        : "group chat";
  const ChannelIcon = !channel
    ? MessageCircle
    : channel.type === ChannelType.Channel
      ? channel.isPrivate
        ? Lock
        : Hash
      : Users2;

  const body = (payload.body ?? "").trim();
  const preview = body.length > 140 ? `${body.slice(0, 140)}…` : body;

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        // Sized to feel like a chat-message preview, not a system note.
        "fsh-chat-toast relative w-[380px] overflow-hidden rounded-xl",
        "border border-[var(--color-border)] bg-[var(--color-card)]",
        "shadow-[var(--shadow-lift)]",
      )}
    >
      {/* Atmospheric brand wash on the leading edge — mirrors the
          chat-channel-header pseudo so a toast reads as part of the
          chat family, not the .fsh-toast tone-rail family. */}
      <span
        aria-hidden
        className="pointer-events-none absolute inset-y-0 left-0 w-1 bg-[var(--color-primary)] opacity-90"
      />
      <span
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            "radial-gradient(ellipse 50% 80% at 0% 50%, oklch(from var(--color-primary) l c h / 0.10), transparent 65%)",
        }}
      />

      {/* The card body — clickable for navigation. The dismiss button is
          rendered as a sibling so its click can stopPropagation cleanly. */}
      <button
        type="button"
        onClick={onView}
        title="Open conversation"
        className={cn(
          "relative flex w-full items-start gap-3 p-3 pr-9 text-left",
          "cursor-pointer transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.04)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        )}
      >
        <Avatar
          name={author.name}
          src={author.imageUrl ?? null}
          size="md"
          className="shrink-0"
        />

        <div className="min-w-0 flex-1">
          {/* Author row */}
          <div className="flex items-center gap-2">
            <span className="truncate text-[13.5px] font-semibold tracking-tight text-[var(--color-foreground)]">
              {author.name}
            </span>
            <span
              aria-hidden
              className="h-1 w-1 shrink-0 rounded-full bg-[var(--color-primary)] opacity-70"
            />
            <span className="truncate text-[11px] text-[var(--color-muted-foreground)]">
              just now
            </span>
          </div>

          {/* Channel context */}
          <div className="mt-0.5 flex items-center gap-1.5 text-[var(--color-muted-foreground)]">
            <ChannelIcon className="h-3 w-3 shrink-0" aria-hidden />
            <span className="truncate text-[11.5px]">
              {channelLabel}
            </span>
          </div>

          {/* Body preview */}
          {preview ? (
            <p
              className={cn(
                "mt-2 line-clamp-2 text-[13px] leading-relaxed text-[var(--color-foreground)]",
                // Subtle italic so the preview reads as "quoted" content
                // rather than a UI label.
                "[font-feature-settings:'ss01','cv11']",
              )}
            >
              {preview}
            </p>
          ) : (
            <p className="mt-2 text-[12px] italic text-[var(--color-muted-foreground)]">
              (attachment or empty body)
            </p>
          )}
        </div>
      </button>

      {/* Dismiss — top-right, sibling to the click target so its onClick
          short-circuits the card's onView. */}
      <button
        type="button"
        onClick={(e) => {
          e.stopPropagation();
          onDismiss();
        }}
        aria-label="Dismiss notification"
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
