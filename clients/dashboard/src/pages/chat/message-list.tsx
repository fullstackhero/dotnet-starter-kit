import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useVirtualizer } from "@tanstack/react-virtual";
import { ChevronDown } from "lucide-react";
import { listChannelMessages, type MessageDto } from "@/api/chat";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { canMerge, dayKey, dayRuleLabel } from "@/pages/chat/chat-utils";
import { Message } from "@/pages/chat/message";

type Row =
  | { kind: "day"; key: string; label: string }
  | { kind: "message"; key: string; message: MessageDto; merged: boolean }
  | { kind: "unread"; key: string };

const PINNED_THRESHOLD = 200;

/**
 * Virtualised message stream. We keep the full materialised list in cache
 * (TanStack Query), then derive a flat row array that interleaves day-rule
 * separators between message groups before handing it to react-virtual. New
 * messages from SignalR land into the cache via setQueryData.
 *
 * Auto-scroll behaviour: pin to bottom by default; if the user has scrolled
 * up beyond PINNED_THRESHOLD, suppress auto-scroll and track an "unseen"
 * counter that surfaces a jump-to-bottom pill. The unread watermark is
 * snapshotted once per channel session so it doesn't move as mark-read fires.
 */
export function MessageList({
  channelId,
  selfUserId,
  lastReadMessageId,
  onReply,
}: {
  channelId: string;
  selfUserId?: string;
  /** Caller's lastReadMessageId on the channel — used to place the unread divider. */
  lastReadMessageId?: string | null;
  onReply?: (parentMessageId: string) => void;
}) {
  const queryClient = useQueryClient();
  const queryKey = ["chat", "messages", channelId];

  const messagesQuery = useQuery({
    queryKey,
    queryFn: () => listChannelMessages(channelId, { pageSize: 100 }),
    staleTime: 0,
  });

  // Cache returns newest-first (Guid v7 desc); reverse for chronological display.
  const messages = useMemo(() => (messagesQuery.data ?? []).slice().reverse(), [messagesQuery.data]);

  // Subscribe to realtime hub events targeting this channel and patch cache.
  useRealtimeEvent<MessageDto>(
    "ChatMessageCreated",
    (payload) => {
      if (payload.channelId !== channelId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) => {
        if (!prev) return [payload];
        if (prev.some((m) => m.id === payload.id)) return prev;
        return [payload, ...prev];
      });
    },
    [channelId],
  );

  useRealtimeEvent<MessageDto>(
    "ChatMessageEdited",
    (payload) => {
      if (payload.channelId !== channelId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) =>
        prev?.map((m) => (m.id === payload.id ? payload : m)),
      );
    },
    [channelId],
  );

  useRealtimeEvent<{ channelId: string; messageId: string }>(
    "ChatMessageDeleted",
    (payload) => {
      if (payload.channelId !== channelId) return;
      void queryClient.invalidateQueries({ queryKey });
    },
    [channelId],
  );

  useRealtimeEvent<{ channelId: string; messageId: string }>(
    "ChatReactionChanged",
    (payload) => {
      if (payload.channelId !== channelId) return;
      void queryClient.invalidateQueries({ queryKey });
    },
    [channelId],
  );

  // Latch the unread watermark exactly once per channel session. We can't use
  // a useEffect for this because the rows useMemo below needs the value during
  // the same render that channelId changes, otherwise the divider flickers in
  // the wrong spot for one frame. Mutating a ref during render is sound when
  // the mutation is keyed on a prop change.
  const watermarkRef = useRef<{ channelId: string; messageId: string | null }>({
    channelId: "",
    messageId: null,
  });
  if (watermarkRef.current.channelId !== channelId && lastReadMessageId !== undefined) {
    watermarkRef.current = { channelId, messageId: lastReadMessageId ?? null };
  }
  const watermark =
    watermarkRef.current.channelId === channelId ? watermarkRef.current.messageId : null;

  const rows = useMemo<Row[]>(() => {
    const out: Row[] = [];
    let lastDay = "";
    let prevMessage: MessageDto | null = null;

    // Only insert the divider when the watermark is in the loaded window AND
    // there are messages newer than it — otherwise the divider would land on
    // the bottom (nothing unread) or off-screen (all unread, fallback below).
    const watermarkIndex = watermark
      ? messages.findIndex((m) => m.id === watermark)
      : -1;
    const insertDividerAfter =
      watermarkIndex >= 0 && watermarkIndex < messages.length - 1 ? watermarkIndex : -1;

    for (let i = 0; i < messages.length; i++) {
      const m = messages[i];
      const k = dayKey(m.createdAtUtc);
      if (k !== lastDay) {
        out.push({ kind: "day", key: `day-${k}`, label: dayRuleLabel(m.createdAtUtc) });
        lastDay = k;
        prevMessage = null;
      }
      const merged = prevMessage !== null && canMerge(prevMessage, m);
      out.push({ kind: "message", key: m.id, message: m, merged });
      prevMessage = m;

      if (i === insertDividerAfter) {
        out.push({ kind: "unread", key: "unread-divider" });
        prevMessage = null; // first unread starts a fresh author block
      }
    }
    return out;
  }, [messages, watermark]);

  const parentRef = useRef<HTMLDivElement | null>(null);
  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: (index) => {
      const r = rows[index];
      if (r?.kind === "day") return 32;
      if (r?.kind === "unread") return 36;
      return 64;
    },
    overscan: 8,
  });

  // Jump-to-bottom pill state. Unseen counter only ticks when a new message
  // lands while the user is scrolled away from the bottom.
  const [unseenCount, setUnseenCount] = useState(0);
  const prevLastIdRef = useRef<string | undefined>(undefined);
  const lastMessageId = messages.at(-1)?.id;

  useEffect(() => {
    const el = parentRef.current;
    if (!el || rows.length === 0) return;
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    const isNewMessage =
      prevLastIdRef.current !== undefined && prevLastIdRef.current !== lastMessageId;
    prevLastIdRef.current = lastMessageId;

    if (distanceFromBottom < PINNED_THRESHOLD) {
      virtualizer.scrollToIndex(rows.length - 1, { align: "end" });
      setUnseenCount(0);
    } else if (isNewMessage) {
      setUnseenCount((c) => c + 1);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lastMessageId]);

  // Reset the channel session — wipe the unseen counter when switching channels.
  useEffect(() => {
    setUnseenCount(0);
    prevLastIdRef.current = undefined;
  }, [channelId]);

  const onScroll = useCallback(() => {
    const el = parentRef.current;
    if (!el) return;
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    if (distanceFromBottom < 80) setUnseenCount(0);
  }, []);

  const jumpToBottom = useCallback(() => {
    if (rows.length === 0) return;
    virtualizer.scrollToIndex(rows.length - 1, { align: "end" });
    setUnseenCount(0);
  }, [virtualizer, rows.length]);

  if (messagesQuery.isLoading) {
    return (
      <div className="flex h-full items-center justify-center px-6">
        <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          Loading messages…
        </p>
      </div>
    );
  }

  if (messages.length === 0) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-2 px-6 text-center">
        <p className="text-display text-base font-semibold tracking-tight">
          No messages yet
        </p>
        <p className="max-w-sm text-sm text-[var(--color-muted-foreground)]">
          This is the very beginning of the conversation. Send the first message to break the silence.
        </p>
      </div>
    );
  }

  return (
    <div className="relative h-full">
      <div ref={parentRef} onScroll={onScroll} className="h-full overflow-y-auto">
        <div style={{ height: virtualizer.getTotalSize(), position: "relative" }}>
          {virtualizer.getVirtualItems().map((virtualRow) => {
            const row = rows[virtualRow.index];
            if (!row) return null;
            return (
              <div
                key={row.key}
                data-index={virtualRow.index}
                ref={virtualizer.measureElement}
                style={{
                  position: "absolute",
                  top: 0,
                  left: 0,
                  right: 0,
                  transform: `translateY(${virtualRow.start}px)`,
                }}
              >
                {row.kind === "day" ? (
                  <div className="px-4">
                    <div className="chat-day-rule">
                      <span>{row.label}</span>
                    </div>
                  </div>
                ) : row.kind === "unread" ? (
                  <div className="px-4">
                    <div className="chat-unread-divider">
                      <span>New</span>
                    </div>
                  </div>
                ) : (
                  <Message
                    message={row.message}
                    selfUserId={selfUserId}
                    isMerged={row.merged}
                    onReply={onReply}
                  />
                )}
              </div>
            );
          })}
        </div>
      </div>

      {unseenCount > 0 && (
        <div className="pointer-events-none absolute inset-x-0 bottom-3 z-10 flex justify-center">
          <button
            type="button"
            onClick={jumpToBottom}
            className="chat-jump-pill pointer-events-auto"
            aria-label={`Jump to latest, ${unseenCount} unseen ${unseenCount === 1 ? "message" : "messages"}`}
          >
            <span className="chat-jump-pill-count" aria-hidden>
              {unseenCount > 99 ? "99+" : unseenCount}
            </span>
            <span>{unseenCount === 1 ? "new message" : "new messages"}</span>
            <ChevronDown className="h-3.5 w-3.5" aria-hidden />
          </button>
        </div>
      )}
    </div>
  );
}
