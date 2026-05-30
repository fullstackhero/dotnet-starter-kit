import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
// Per-route chat chrome — Vite extracts this into the chat-page CSS chunk
// so other pages stop shipping the unread divider / day rule / reaction
// chip / jump-pill / mention pill / typing dot rules they'll never use.
import "./chat.css";
import {
  ArrowLeft,
  Hash,
  Lock,
  MessageCircle,
  Search,
  Settings,
  Users2,
} from "lucide-react";
import { toast } from "sonner";
import {
  ChannelType,
  getChannelById,
  listChannelMessages,
  listMyChannels,
  markChannelRead,
  type MessageDto,
} from "@/api/chat";
import { useAuth } from "@/auth/use-auth";
import { ChannelRail } from "@/pages/chat/channel-rail";
import { ChannelSettingsDialog } from "@/pages/chat/channel-settings";
import { ChatPinnedBar } from "@/pages/chat/chat-pinned";
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
import { useRealtime } from "@/realtime/realtime-context";

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

  const channels = useMemo(() => channelsQuery.data ?? [], [channelsQuery.data]);

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
        hasActiveChannel={Boolean(routeChannelId)}
      />
      <main
        className={cn(
          "min-w-0 flex-1 flex-col bg-[var(--color-background)]",
          // Mobile: only one of rail / main is visible at a time. With an
          // active channel selected, the main column takes over and the
          // rail collapses; without one, the rail fills the screen.
          routeChannelId ? "flex" : "hidden md:flex",
        )}
      >
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
    <div className="flex h-full items-center justify-center px-6">
      <div className="flex flex-col items-center text-center">
        <div className="mb-4 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
          <MessageCircle className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
        </div>
        <h3 className="mb-1.5 font-display text-[17px] font-semibold text-[var(--color-foreground)]">
          Pick a conversation
        </h3>
        <p className="mb-6 max-w-[360px] text-[13px] text-[var(--color-muted-foreground)]">
          Choose a channel on the left to jump in. Channels are public to your
          tenant; DMs are private to the people in them. Mentions land in the
          notification bell, top right.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-2">
          <KeyHint label="Send" combo="↵" />
          <KeyHint label="Newline" combo="⇧↵" />
          <KeyHint label="Mention" combo="@" />
        </div>
      </div>
    </div>
  );
}

function KeyHint({ label, combo }: { label: string; combo: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-card)] px-2 py-0.5 text-[11px] text-[var(--color-muted-foreground)]">
      <span className="font-semibold text-[var(--color-foreground)]">{combo}</span>
      <span>{label}</span>
    </span>
  );
}

function ActiveChannel({
  channelId,
  selfUserId,
}: {
  channelId: string;
  selfUserId?: string;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [replyTo, setReplyTo] = useState<MessageDto | null>(null);
  const [searching, setSearching] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const messageListRef = useRef<MessageListHandle | null>(null);
  const { invoke, status } = useRealtime();

  // Join this channel's realtime group whenever it's opened, and re-join when the
  // socket (re)connects. AppHub.OnConnectedAsync only pre-joins channels that existed
  // at connect time, so a DM/channel opened later — or created after the socket came
  // up — wouldn't receive live messages until a reload without this. The hub method is
  // membership-gated and idempotent, so calling it on every open/reconnect is safe.
  useEffect(() => {
    if (status !== "connected") return;
    void invoke("JoinChannel", channelId);
  }, [channelId, status, invoke]);

  // Clear ephemeral state when the user switches channels.
  useEffect(() => {
    setReplyTo(null);
    setSearching(false);
    setSettingsOpen(false);
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
    channel?.type === ChannelType.DirectMessage
      ? channel.members.find((m) => m.userId !== selfUserId)
      : null;
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
    // Skip optimistic temp messages — the server can't parse "temp:xxx" as
    // a Guid and the request 500s. Real id arrives via realtime; this
    // effect re-fires then.
    if (latestMessageId.startsWith("temp:")) return;
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
        <p className="text-[12px] text-[var(--color-muted-foreground)]">
          Loading channel…
        </p>
      </div>
    );
  }

  const title =
    channel.type === ChannelType.DirectMessage && otherDmMember
      ? dmPartner.name
      : channelTitle(channel, selfUserId);
  const Icon =
    channel.type === ChannelType.Channel ? (channel.isPrivate ? Lock : Hash) : Users2;

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
            "flex h-12 shrink-0 items-center gap-2 border-b border-[var(--color-border)] bg-[oklch(from_var(--color-background)_l_c_h_/_0.8)] px-3 backdrop-blur-sm md:gap-3 md:px-4",
          )}
        >
          {/* Mobile back button — returns to the rail-only view. */}
          <button
            type="button"
            onClick={() => navigate("/chat")}
            aria-label="Back to channels"
            title="Back to channels"
            className="grid size-9 shrink-0 cursor-pointer place-items-center rounded-lg text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)] md:hidden"
          >
            <ArrowLeft className="size-4" />
          </button>
          <span
            aria-hidden
            className="hidden size-7 shrink-0 place-items-center rounded-md bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)] md:grid"
          >
            <Icon className="size-3.5" />
          </span>
          <div className="min-w-0 flex-1">
            <h2 className="truncate font-display text-[14px] font-semibold tracking-tight text-[var(--color-foreground)]">
              {title}
            </h2>
            {channel.description && channel.type === ChannelType.Channel && (
              <p className="truncate text-[11px] text-[var(--color-muted-foreground)]">
                {channel.description}
              </p>
            )}
          </div>
          <span className="hidden text-[11px] tabular-nums text-[var(--color-muted-foreground)] md:inline">
            {channel.members.length}{" "}
            {channel.members.length === 1 ? "member" : "members"}
          </span>
          <button
            type="button"
            onClick={() => setSearching(true)}
            aria-label="Search messages"
            title="Search messages"
            className="grid size-8 cursor-pointer place-items-center rounded-lg text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]"
          >
            <Search className="size-3.5" />
          </button>
          {channel.type === ChannelType.Channel && (
            <button
              type="button"
              onClick={() => setSettingsOpen(true)}
              aria-label="Channel settings"
              title="Channel settings"
              className="grid size-8 cursor-pointer place-items-center rounded-lg text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]"
            >
              <Settings className="size-3.5" />
            </button>
          )}
        </header>
      )}

      <ChannelSettingsDialog
        open={settingsOpen}
        onOpenChange={setSettingsOpen}
        channel={channel}
        selfUserId={selfUserId}
      />

      {/* Pinned-messages bar — slim strip under the header; hidden when the
          channel has nothing pinned. Click to review/jump to pins. */}
      <ChatPinnedBar
        channelId={channelId}
        onJump={(id) => {
          const ok = messageListRef.current?.jumpToMessage(id) ?? false;
          if (!ok) {
            toast.info("That message is older than the loaded window.");
          }
        }}
      />

      {/* Message list — fills the remaining height. */}
      <div className="min-h-0 flex-1">
        <MessageList
          ref={messageListRef}
          channelId={channelId}
          selfUserId={selfUserId}
          lastReadMessageId={lastReadMessageId}
          members={channel.members}
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
