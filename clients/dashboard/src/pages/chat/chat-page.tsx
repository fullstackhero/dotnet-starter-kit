import { useEffect, useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Hash, Lock, MessageCircle, Users2 } from "lucide-react";
import { getChannelById, listChannelMessages, listMyChannels, markChannelRead } from "@/api/chat";
import { useAuth } from "@/auth/use-auth";
import { ChannelRail } from "@/pages/chat/channel-rail";
import { Composer } from "@/pages/chat/composer";
import { MessageList } from "@/pages/chat/message-list";
import { TypingIndicator } from "@/pages/chat/typing-indicator";
import { channelTitle } from "@/pages/chat/chat-utils";
import { cn } from "@/lib/cn";

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
    <div className="flex h-full flex-col items-center justify-center gap-3 px-6">
      <span
        aria-hidden
        className="grid h-12 w-12 place-items-center rounded-xl bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
      >
        <MessageCircle className="h-5 w-5" />
      </span>
      <p className="text-display text-base font-semibold tracking-tight">
        Select a channel
      </p>
      <p className="max-w-md text-center text-sm text-[var(--color-muted-foreground)]">
        Pick a conversation on the left, or create a channel to start one.
      </p>
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

  const title = channelTitle(channel, selfUserId);
  const Icon =
    channel.type === 2 ? (channel.isPrivate ? Lock : Hash) : Users2;

  return (
    <div className="flex h-full min-h-0 flex-col">
      {/* Channel header — atmospheric brand glow tucked under the title. */}
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
        <span className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2 py-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          <span className="tabular-nums">{channel.members.length}</span>{" "}
          {channel.members.length === 1 ? "member" : "members"}
        </span>
      </header>

      {/* Message list — fills the remaining height. */}
      <div className="min-h-0 flex-1">
        <MessageList channelId={channelId} selfUserId={selfUserId} />
      </div>

      {/* Typing presence row — reserved height so the composer doesn't jump. */}
      <TypingIndicator channelId={channelId} selfUserId={selfUserId} />

      {/* Composer plinth — brand-tinted on focus. */}
      <Composer channelId={channelId} channelTitle={title} />
    </div>
  );
}
