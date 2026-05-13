import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Hash, Lock, MessageCircle, Search, Users2 } from "lucide-react";
import { toast } from "sonner";
import {
  getChannelById,
  listChannelMessages,
  listMyChannels,
  markChannelRead,
  type MessageDto,
} from "@/api/chat";
import { useAuth } from "@/auth/use-auth";
import { ChannelRail } from "@/pages/chat/channel-rail";
import { ChatSearchOverlay } from "@/pages/chat/chat-search";
import { Composer } from "@/pages/chat/composer";
import {
  MessageList,
  type MessageListHandle,
} from "@/pages/chat/message-list";
import { TypingIndicator } from "@/pages/chat/typing-indicator";
import { channelTitle } from "@/pages/chat/chat-utils";
import { cn } from "@/lib/cn";
import { useUserDisplay } from "@/lib/use-user-display";

/**
 * /chat — top-level chat shell.
 *
 * Two-column desktop: ChannelRail (left) + active channel pane (right).
 * The active channel is taken from the :channelId route param; if absent,
 * pre-select the first channel and replace the URL so deep-linking works.
 *
 * NOTE: not yet routed through the dashboard's standard p-4/md:p-6 main
 * padding — the chat surface fills its container edge-to-edge so the
 * channel rail can extend the full height of <main>. The page itself
 * compensates via its own internal padding on the right column.
 */
export function ChatPage() {
  const { channelId: routeChannelId } = useParams<{ channelId?: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const channelsQuery = useQuery({
    queryKey: ["chat", "my-channels"],
    queryFn: () => listMyChannels({ pageSize: 100 }),
    staleTime: 30_000,
  });

  const channels = channelsQuery.data ?? [];

  // Auto-pick the first channel when no channelId is in the route.
  useEffect(() => {
    if (routeChannelId) return;
    if (channels.length === 0) return;
    navigate(`/chat/${channels[0].id}`, { replace: true });
  }, [routeChannelId, channels, navigate]);

  return (
    <div
      className={cn(
        // Override the default <main> padding so the rail and message area
        // can extend full-bleed. The route registration sets the content
        // height to fill the viewport via flex-1 on <main>.
        "-m-4 flex h-[calc(100vh-3.5rem)] min-h-0 flex-1 overflow-hidden md:-m-6",
        "rounded-none border-0",
      )}
    >
      <ChannelRail
        selectedChannelId={routeChannelId}
        onSelect={(id) => navigate(`/chat/${id}`)}
        selfUserId={user?.id}
      />
      <main className="flex min-w-0 flex-1 flex-col bg-[var(--color-surface-1)]">
        {routeChannelId ? (
          <ActiveChannel channelId={routeChannelId} selfUserId={user?.id} />
        ) : (
          <EmptyState />
        )}
      </main>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="flex h-full items-center justify-center p-6 md:p-10">
      <div className="chat-empty-hero relative flex max-w-lg flex-col items-start gap-4 text-left">
        <span
          aria-hidden
          className="grid h-12 w-12 place-items-center rounded-xl bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-1 ring-[oklch(from_var(--color-primary)_l_c_h_/_0.25)]"
        >
          <MessageCircle className="h-5 w-5" />
        </span>
        <div className="flex flex-col gap-1.5">
          <p className="font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-primary)]">
            FSH · Chat
          </p>
          <h2 className="text-display text-2xl font-semibold leading-tight tracking-tight">
            Pick a conversation
            <br />
            or start one.
          </h2>
          <p className="text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            Choose a channel on the left to jump in. Channels are public to your
            tenant; DMs are private to the people in them. Mentions land in the
            notification bell, top right.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2 pt-1">
          <span className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2 py-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            ↵ Send
          </span>
          <span className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2 py-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            ⇧↵ Newline
          </span>
          <span className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2 py-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            @ Mention
          </span>
        </div>
      </div>
    </div>
  );
}

function ActiveChannel({
  channelId,
  selfUserId,
}: {
  channelId: string;
  selfUserId?: string;
}) {
  const queryClient = useQueryClient();
  const [replyTo, setReplyTo] = useState<MessageDto | null>(null);
  const [searching, setSearching] = useState(false);
  const messageListRef = useRef<MessageListHandle | null>(null);

  // Clear ephemeral state when the user switches channels.
  useEffect(() => {
    setReplyTo(null);
    setSearching(false);
  }, [channelId]);

  const channelQuery = useQuery({
    queryKey: ["chat", "channel", channelId],
    queryFn: () => getChannelById(channelId),
    staleTime: 30_000,
  });

  const messagesQuery = useQuery({
    queryKey: ["chat", "messages", channelId],
    queryFn: () => listChannelMessages(channelId, { pageSize: 100 }),
    staleTime: 0,
  });

  const channel = channelQuery.data;
  const latestMessageId = useMemo(() => messagesQuery.data?.[0]?.id, [messagesQuery.data]);
  const selfMember = channel?.members.find((m) => m.userId === selfUserId);
  const lastReadMessageId = selfMember?.lastReadMessageId ?? null;
  // For 1-on-1 DMs, resolve the other member's real name so the header +
  // composer placeholder show "Alice Anderson" instead of "@4d3a45fc".
  const otherDmMember =
    channel?.type === 0 ? channel.members.find((m) => m.userId !== selfUserId) : null;
  const dmPartner = useUserDisplay(otherDmMember?.userId);

  // Mark-read effect — every time the latest message id changes (new
  // messages land via realtime), advance the watermark. We swallow errors
  // because this is best-effort housekeeping.
  const markReadMutation = useMutation({
    mutationFn: ({ id }: { id: string }) => markChannelRead(channelId, id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      void queryClient.invalidateQueries({ queryKey: ["notifications", "unread-count"] });
    },
  });

  useEffect(() => {
    if (!latestMessageId) return;
    markReadMutation.mutate({ id: latestMessageId });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [latestMessageId, channelId]);

  if (channelQuery.isError) {
    return (
      <div className="flex h-full items-center justify-center px-6 text-center">
        <p className="text-sm text-[var(--color-muted-foreground)]">
          That channel isn't reachable. It may have been archived or you're no longer a member.
        </p>
      </div>
    );
  }

  if (!channel) {
    return (
      <div className="flex h-full items-center justify-center">
        <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          Loading channel…
        </p>
      </div>
    );
  }

  const title =
    channel.type === 0 && otherDmMember ? dmPartner.name : channelTitle(channel, selfUserId);
  const Icon =
    channel.type === 2 ? (channel.isPrivate ? Lock : Hash) : Users2;

  return (
    <div className="relative flex h-full min-h-0 flex-col">
      {searching ? (
        <ChatSearchOverlay
          channelId={channelId}
          onClose={() => setSearching(false)}
          onJump={(id) => {
            const ok = messageListRef.current?.jumpToMessage(id) ?? false;
            if (!ok) {
              toast.info("That message is older than the loaded window.");
            }
          }}
        />
      ) : (
        <header
          className={cn(
            "chat-channel-header flex h-14 shrink-0 items-center gap-3 border-b border-[var(--color-border)] px-4",
          )}
        >
          <span
            aria-hidden
            className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[var(--color-surface-3)] text-[var(--color-muted-foreground)]"
          >
            <Icon className="h-3.5 w-3.5" />
          </span>
          <div className="min-w-0 flex-1">
            <h1 className="text-display truncate text-sm font-semibold tracking-tight">
              {title}
            </h1>
            {channel.description && channel.type === 2 && (
              <p className="truncate text-[11px] text-[var(--color-muted-foreground)]">
                {channel.description}
              </p>
            )}
          </div>
          <button
            type="button"
            onClick={() => setSearching(true)}
            aria-label="Search messages"
            title="Search messages"
            className={cn(
              "grid h-7 w-7 cursor-pointer place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            )}
          >
            <Search className="h-3.5 w-3.5" />
          </button>
          <span className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2 py-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
            <span className="tabular-nums">{channel.members.length}</span>{" "}
            {channel.members.length === 1 ? "member" : "members"}
          </span>
        </header>
      )}

      {/* Message list — fills the remaining height. */}
      <div className="min-h-0 flex-1">
        <MessageList
          ref={messageListRef}
          channelId={channelId}
          selfUserId={selfUserId}
          lastReadMessageId={lastReadMessageId}
          onReply={setReplyTo}
        />
      </div>

      {/* Typing presence row — reserved height so the composer doesn't jump. */}
      <TypingIndicator channelId={channelId} selfUserId={selfUserId} />

      {/* Composer plinth — brand-tinted on focus. Renders a quoted preview
          when replyTo is set; clearing it returns the composer to normal. */}
      <Composer
        channelId={channelId}
        channelTitle={title}
        channelType={channel.type}
        selfUserId={selfUserId}
        replyTo={replyTo}
        onClearReply={() => setReplyTo(null)}
      />
    </div>
  );
}
