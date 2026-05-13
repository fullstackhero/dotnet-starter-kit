import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { useVirtualizer } from "@tanstack/react-virtual";
import { ChevronDown } from "lucide-react";
import {
  listChannelMessages,
  listMessageReplies,
  type MessageDto,
} from "@/api/chat";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { cn } from "@/lib/cn";
import { canMerge, dayKey, dayRuleLabel } from "@/pages/chat/chat-utils";
import { Message } from "@/pages/chat/message";

type Row =
  | { kind: "day"; key: string; label: string }
  | {
      kind: "message";
      key: string;
      message: MessageDto;
      merged: boolean;
      isExpanded: boolean;
    }
  | {
      kind: "reply";
      key: string;
      message: MessageDto;
      merged: boolean;
      parentId: string;
      isLast: boolean;
    }
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
  /** Sets the composer's reply context. The composer renders the quote and
   *  posts the next send with parentMessageId = parent.id. Teams-DM style. */
  onReply?: (parent: MessageDto) => void;
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
  // Replies (parentMessageId set) are kept out of the top-level cache — they
  // belong in the per-parent ["chat","replies",parentId] cache.
  useRealtimeEvent<MessageDto>(
    "ChatMessageCreated",
    (payload) => {
      if (payload.channelId !== channelId) return;
      if (payload.parentMessageId) {
        // Bump the parent's replyCount in the top-level cache so the chip appears.
        queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) =>
          prev?.map((m) =>
            m.id === payload.parentMessageId
              ? { ...m, replyCount: (m.replyCount ?? 0) + 1 }
              : m,
          ),
        );
        // Append into the replies cache if the thread is loaded (i.e. expanded).
        queryClient.setQueryData<MessageDto[] | undefined>(
          ["chat", "replies", payload.parentMessageId],
          (prev) => {
            if (!prev) return prev; // not loaded yet → wait for the user to expand
            if (prev.some((m) => m.id === payload.id)) return prev;
            return [...prev, payload];
          },
        );
        return;
      }
      queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) => {
        if (!prev) return [payload];
        if (prev.some((m) => m.id === payload.id)) return prev;
        return [payload, ...prev];
      });
    },
    [channelId, queryKey, queryClient],
  );

  useRealtimeEvent<MessageDto>(
    "ChatMessageEdited",
    (payload) => {
      if (payload.channelId !== channelId) return;
      // Patch top-level cache (no-op if id isn't a top-level message).
      queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) =>
        prev?.map((m) => (m.id === payload.id ? payload : m)),
      );
      if (payload.parentMessageId) {
        queryClient.setQueryData<MessageDto[] | undefined>(
          ["chat", "replies", payload.parentMessageId],
          (prev) => prev?.map((m) => (m.id === payload.id ? payload : m)),
        );
      }
    },
    [channelId, queryKey, queryClient],
  );

  useRealtimeEvent<{ channelId: string; messageId: string }>(
    "ChatMessageDeleted",
    (payload) => {
      if (payload.channelId !== channelId) return;
      void queryClient.invalidateQueries({ queryKey });
      // The deleted message might be a reply — invalidate all loaded threads
      // for this channel. Cheap: there are usually 0-3 expanded threads.
      void queryClient.invalidateQueries({ queryKey: ["chat", "replies"] });
    },
    [channelId, queryKey, queryClient],
  );

  useRealtimeEvent<{ channelId: string; messageId: string }>(
    "ChatReactionChanged",
    (payload) => {
      if (payload.channelId !== channelId) return;
      void queryClient.invalidateQueries({ queryKey });
      void queryClient.invalidateQueries({ queryKey: ["chat", "replies"] });
    },
    [channelId, queryKey, queryClient],
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

  // ── Inline thread expansion (Teams-channel style) ─────────────────────
  // Click the "N replies" chip → fetch this parent's replies and render
  // them indented under the parent. Each toggle adds/removes the parentId
  // from this set; useQueries below tracks one query per expanded parent.
  const [expandedThreads, setExpandedThreads] = useState<Set<string>>(new Set());

  const toggleThread = useCallback((parentMessageId: string) => {
    setExpandedThreads((prev) => {
      const next = new Set(prev);
      if (next.has(parentMessageId)) next.delete(parentMessageId);
      else next.add(parentMessageId);
      return next;
    });
  }, []);

  // Reset expansion when switching channels — replies are channel-local.
  useEffect(() => {
    setExpandedThreads(new Set());
  }, [channelId]);

  const expandedIds = useMemo(() => Array.from(expandedThreads), [expandedThreads]);

  const repliesQueries = useQueries({
    queries: expandedIds.map((parentId) => ({
      queryKey: ["chat", "replies", parentId] as const,
      queryFn: () => listMessageReplies(parentId, { pageSize: 100 }),
      staleTime: 0,
    })),
  });

  const repliesByParent = useMemo(() => {
    const map = new Map<string, MessageDto[]>();
    expandedIds.forEach((id, i) => {
      const data = repliesQueries[i]?.data;
      if (data) map.set(id, data);
    });
    return map;
    // repliesQueries is a fresh array on every render; depend on the data
    // refs instead so we don't churn the memo unnecessarily.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [expandedIds, ...repliesQueries.map((q) => q.data)]);

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
      const isExpanded = expandedThreads.has(m.id);
      out.push({ kind: "message", key: m.id, message: m, merged, isExpanded });
      prevMessage = m;

      // Inline-expand replies for this parent if the chip was clicked.
      if (isExpanded) {
        const replies = repliesByParent.get(m.id) ?? [];
        let prevReply: MessageDto | null = null;
        for (let r = 0; r < replies.length; r++) {
          const reply = replies[r];
          const mergedReply = prevReply !== null && canMerge(prevReply, reply);
          out.push({
            kind: "reply",
            key: `${m.id}::${reply.id}`,
            message: reply,
            merged: mergedReply,
            parentId: m.id,
            isLast: r === replies.length - 1,
          });
          prevReply = reply;
        }
        // The next top-level message starts a fresh author block.
        prevMessage = null;
      }

      if (i === insertDividerAfter) {
        out.push({ kind: "unread", key: "unread-divider" });
        prevMessage = null; // first unread starts a fresh author block
      }
    }
    return out;
  }, [messages, watermark, expandedThreads, repliesByParent]);

  const parentRef = useRef<HTMLDivElement | null>(null);
  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: (index) => {
      const r = rows[index];
      if (r?.kind === "day") return 32;
      if (r?.kind === "unread") return 36;
      if (r?.kind === "reply") return 56;
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

  // Land at the latest message on first paint of each channel session.
  // Without this, opening a channel scrolls to the top (oldest message) since
  // the list is chronological top→bottom. The scroll-to-latest is what makes
  // a toast-click "take me to the latest message of that thread" actually
  // land on it. Latched via a ref keyed on channelId so it fires exactly
  // once per channel session, after the first batch of rows has loaded.
  const initializedSessionRef = useRef<string | null>(null);
  useEffect(() => {
    if (rows.length === 0) return;
    if (initializedSessionRef.current === channelId) return;
    initializedSessionRef.current = channelId;
    // Defer one frame so the virtualizer's scroll element has its real
    // height before scrollToIndex runs.
    requestAnimationFrame(() => {
      virtualizer.scrollToIndex(rows.length - 1, { align: "end" });
    });
    prevLastIdRef.current = lastMessageId;
    setUnseenCount(0);
  }, [channelId, rows.length, virtualizer, lastMessageId]);

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
                ) : row.kind === "reply" ? (
                  // Indented reply row — Teams-channel style. The 9-col
                  // left margin matches the parent's avatar gutter so the
                  // connector line aligns under the parent's avatar.
                  <div
                    className={cn(
                      "ml-9 border-l border-[var(--color-border)]",
                      row.isLast && "pb-2",
                    )}
                  >
                    <Message
                      message={row.message}
                      selfUserId={selfUserId}
                      isMerged={row.merged}
                      onReply={onReply}
                    />
                  </div>
                ) : (
                  <Message
                    message={row.message}
                    selfUserId={selfUserId}
                    isMerged={row.merged}
                    onReply={onReply}
                    onToggleReplies={toggleThread}
                    isExpanded={row.isExpanded}
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
