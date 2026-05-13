import { useEffect, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { MessageSquareReply, X } from "lucide-react";
import {
  listMessageReplies,
  type ChannelTypeValue,
  type MessageDto,
} from "@/api/chat";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { Composer } from "@/pages/chat/composer";
import { Message } from "@/pages/chat/message";
import { canMerge } from "@/pages/chat/chat-utils";
import { cn } from "@/lib/cn";

/**
 * Slack-style thread overlay. Floats over the right ~380px of the active
 * channel's main column on desktop; full-bleed on mobile. The MessageList
 * underneath stays full-width (its virtualizer never re-measures).
 *
 * Parent message is read from the channel's message-list cache when
 * available; if it's older than the loaded window we render a tombstone
 * placeholder instead of adding a new endpoint.
 */
export function ThreadPanel({
  channelId,
  channelTitle,
  channelType,
  parentMessageId,
  selfUserId,
  onClose,
}: {
  channelId: string;
  channelTitle: string;
  channelType: ChannelTypeValue;
  parentMessageId: string;
  selfUserId?: string;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const repliesKey = useMemo(
    () => ["chat", "replies", parentMessageId] as const,
    [parentMessageId],
  );

  const parent = useMemo<MessageDto | null>(() => {
    const cached = queryClient.getQueryData<MessageDto[]>([
      "chat",
      "messages",
      channelId,
    ]);
    return cached?.find((m) => m.id === parentMessageId) ?? null;
  }, [queryClient, channelId, parentMessageId]);

  const repliesQuery = useQuery({
    queryKey: repliesKey,
    queryFn: () => listMessageReplies(parentMessageId, { pageSize: 100 }),
    staleTime: 0,
  });

  const replies = useMemo(() => repliesQuery.data ?? [], [repliesQuery.data]);

  // ── Realtime: scope events to this thread ─────────────────────────────
  useRealtimeEvent<MessageDto>(
    "ChatMessageCreated",
    (payload) => {
      if (payload.parentMessageId !== parentMessageId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(repliesKey, (prev) => {
        if (!prev) return [payload];
        if (prev.some((m) => m.id === payload.id)) return prev;
        return [...prev, payload];
      });
    },
    [parentMessageId],
  );

  useRealtimeEvent<MessageDto>(
    "ChatMessageEdited",
    (payload) => {
      if (payload.id === parentMessageId) {
        // Patch the parent in the channel's message-list cache so the
        // tombstone-fallback path also picks up the new body.
        queryClient.setQueryData<MessageDto[] | undefined>(
          ["chat", "messages", channelId],
          (prev) => prev?.map((m) => (m.id === payload.id ? payload : m)),
        );
        return;
      }
      if (payload.parentMessageId !== parentMessageId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(repliesKey, (prev) =>
        prev?.map((m) => (m.id === payload.id ? payload : m)),
      );
    },
    [parentMessageId, channelId],
  );

  useRealtimeEvent<{ channelId: string; messageId: string }>(
    "ChatMessageDeleted",
    (payload) => {
      if (payload.messageId === parentMessageId) {
        onClose();
        return;
      }
      void queryClient.invalidateQueries({ queryKey: repliesKey });
    },
    [parentMessageId, onClose],
  );

  useRealtimeEvent<{ channelId: string; messageId: string }>(
    "ChatReactionChanged",
    (payload) => {
      if (
        payload.messageId !== parentMessageId &&
        !replies.some((r) => r.id === payload.messageId)
      ) {
        return;
      }
      void queryClient.invalidateQueries({ queryKey: repliesKey });
      if (payload.messageId === parentMessageId) {
        void queryClient.invalidateQueries({
          queryKey: ["chat", "messages", channelId],
        });
      }
    },
    [parentMessageId, channelId, replies],
  );

  // Esc to close.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  return (
    <aside
      className={cn(
        "absolute right-0 top-0 z-20 flex h-full w-full flex-col md:w-[380px]",
        "border-l border-[var(--color-border)] bg-[var(--color-surface-1)]",
        "shadow-[-12px_0_28px_-12px_oklch(0_0_0_/_0.18)]",
      )}
      aria-label="Thread"
    >
      <header className="flex h-14 shrink-0 items-center gap-3 border-b border-[var(--color-border)] px-4">
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[var(--color-surface-3)] text-[var(--color-muted-foreground)]"
        >
          <MessageSquareReply className="h-3.5 w-3.5" />
        </span>
        <h2 className="text-display flex-1 truncate text-sm font-semibold tracking-tight">
          Thread
        </h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Close thread"
          className={cn(
            "grid h-7 w-7 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          )}
        >
          <X className="h-3.5 w-3.5" />
        </button>
      </header>

      <div className="min-h-0 flex-1 overflow-y-auto">
        {parent ? (
          <Message message={parent} selfUserId={selfUserId} isMerged={false} />
        ) : (
          <div className="px-4 py-3 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            Parent message unavailable.
          </div>
        )}

        <div className="px-4">
          <div className="chat-day-rule">
            <span>
              {replies.length === 0
                ? "Start the thread"
                : `${replies.length} ${replies.length === 1 ? "reply" : "replies"}`}
            </span>
          </div>
        </div>

        {repliesQuery.isLoading ? (
          <div className="px-4 py-3 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            Loading replies…
          </div>
        ) : (
          replies.map((reply, i) => {
            const prev = i > 0 ? replies[i - 1] : null;
            const merged = prev !== null && canMerge(prev, reply);
            return (
              <Message
                key={reply.id}
                message={reply}
                selfUserId={selfUserId}
                isMerged={merged}
              />
            );
          })
        )}
      </div>

      <Composer
        channelId={channelId}
        channelTitle={channelTitle}
        channelType={channelType}
        parentMessageId={parentMessageId}
      />
    </aside>
  );
}
