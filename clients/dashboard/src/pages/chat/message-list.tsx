import { useEffect, useMemo, useRef } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useVirtualizer } from "@tanstack/react-virtual";
import { listChannelMessages, type MessageDto } from "@/api/chat";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { canMerge, dayKey, dayRuleLabel } from "@/pages/chat/chat-utils";
import { Message } from "@/pages/chat/message";

type Row =
  | { kind: "day"; key: string; label: string }
  | { kind: "message"; key: string; message: MessageDto; merged: boolean };

/**
 * Virtualised message stream. We keep the full materialised list in cache
 * (TanStack Query), then derive a flat row array that interleaves day-rule
 * separators between message groups before handing it to react-virtual. New
 * messages from SignalR land into the cache via setQueryData.
 *
 * Auto-scroll behaviour: pin to bottom by default; if the user has scrolled
 * up by more than 200px we *don't* auto-scroll on incoming messages so they
 * can read in peace.
 */
export function MessageList({
  channelId,
  selfUserId,
  onReply,
}: {
  channelId: string;
  selfUserId?: string;
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

  const rows = useMemo<Row[]>(() => {
    const out: Row[] = [];
    let lastDay = "";
    let prevMessage: MessageDto | null = null;
    for (const m of messages) {
      const k = dayKey(m.createdAtUtc);
      if (k !== lastDay) {
        out.push({ kind: "day", key: `day-${k}`, label: dayRuleLabel(m.createdAtUtc) });
        lastDay = k;
        prevMessage = null;
      }
      const merged = prevMessage !== null && canMerge(prevMessage, m);
      out.push({ kind: "message", key: m.id, message: m, merged });
      prevMessage = m;
    }
    return out;
  }, [messages]);

  const parentRef = useRef<HTMLDivElement | null>(null);
  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: (index) => (rows[index]?.kind === "day" ? 32 : 64),
    overscan: 8,
  });

  // Auto-scroll on new messages when the user is already pinned to the bottom.
  const lastMessageId = messages.at(-1)?.id;
  useEffect(() => {
    const el = parentRef.current;
    if (!el || rows.length === 0) return;
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    if (distanceFromBottom < 200) {
      virtualizer.scrollToIndex(rows.length - 1, { align: "end" });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lastMessageId]);

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
    <div ref={parentRef} className="h-full overflow-y-auto">
      <div
        style={{ height: virtualizer.getTotalSize(), position: "relative" }}
      >
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
  );
}
