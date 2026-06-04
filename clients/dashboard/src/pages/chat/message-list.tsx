import {
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from "react";
import { useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { useVirtualizer } from "@tanstack/react-virtual";
import { ChevronDown } from "lucide-react";
import {
  listChannelMessages,
  listMessageReplies,
  type ChannelMemberDto,
  type MessageDto,
} from "@/api/chat";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { canMerge, dayKey, dayRuleLabel } from "@/pages/chat/chat-utils";
import { Message } from "@/pages/chat/message";

export type MessageListHandle = {
  /** Scroll the feed to a message id in the loaded window and flash it.
   *  Returns true if the message was in the loaded window, false otherwise. */
  jumpToMessage(messageId: string): boolean;
};

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
export const MessageList = forwardRef<
  MessageListHandle,
  {
    channelId: string;
    selfUserId?: string;
    /** Caller's lastReadMessageId on the channel — used to place the unread divider. */
    lastReadMessageId?: string | null;
    /** Channel members — drives read receipts under the caller's latest message. */
    members?: ChannelMemberDto[];
    /** Sets the composer's reply context. The composer renders the quote and
     *  posts the next send with parentMessageId = parent.id. Teams-DM style. */
    onReply?: (parent: MessageDto) => void;
  }
>(function MessageList(
  {
    channelId,
    selfUserId,
    lastReadMessageId,
    members,
    onReply,
  },
  ref,
) {
  const queryClient = useQueryClient();
  const queryKey = useMemo(() => ["chat", "messages", channelId] as const, [channelId]);

  const messagesQuery = useQuery({
    queryKey,
    queryFn: () => listChannelMessages(channelId, { pageSize: 100 }),
    staleTime: 0,
  });

  // Cache returns newest-first (Guid v7 desc); reverse for chronological display.
  const messages = useMemo(() => (messagesQuery.data ?? []).slice().reverse(), [messagesQuery.data]);

  // ── Older-message pagination ──────────────────────────────────────────
  // Initial fetch is the newest 100. When the user scrolls near the top of
  // the feed, fetch the next 50 older via the existing `before` cursor and
  // prepend them into the cache (which stores newest-first). We adjust
  // scrollTop by the height delta so the user's visual position doesn't
  // jump — otherwise the near-top trigger would fire again immediately.
  const [hasMoreOlder, setHasMoreOlder] = useState(true);
  const [loadingOlder, setLoadingOlder] = useState(false);

  useEffect(() => {
    if (!messagesQuery.data) return;
    if (messagesQuery.data.length < 100) setHasMoreOlder(false);
  }, [messagesQuery.data]);

  useEffect(() => {
    setHasMoreOlder(true);
    setLoadingOlder(false);
  }, [channelId]);

  const fetchOlder = useCallback(async () => {
    if (loadingOlder || !hasMoreOlder) return;
    const cached = queryClient.getQueryData<MessageDto[]>(queryKey);
    const oldest = cached?.[cached.length - 1];
    if (!oldest) return;
    setLoadingOlder(true);
    const el = parentRef.current;
    const prevScrollHeight = el?.scrollHeight ?? 0;
    const prevScrollTop = el?.scrollTop ?? 0;
    try {
      const older = await listChannelMessages(channelId, {
        before: oldest.id,
        pageSize: 50,
      });
      if (older.length < 50) setHasMoreOlder(false);
      if (older.length > 0) {
        queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) => {
          if (!prev) return older;
          const known = new Set(prev.map((m) => m.id));
          const fresh = older.filter((m) => !known.has(m.id));
          return [...prev, ...fresh];
        });
        // Preserve visual position — virtualizer's measure pass will set
        // a new scrollHeight; re-anchor scrollTop so the same message
        // stays under the user's eye.
        requestAnimationFrame(() => {
          const next = parentRef.current;
          if (!next) return;
          const delta = next.scrollHeight - prevScrollHeight;
          if (delta > 0) next.scrollTop = prevScrollTop + delta;
        });
      }
    } finally {
      setLoadingOlder(false);
    }
  }, [loadingOlder, hasMoreOlder, queryClient, queryKey, channelId]);

  // Subscribe to realtime hub events targeting this channel and patch cache.
  // Replies (parentMessageId set) are kept out of the top-level cache — they
  // belong in the per-parent ["chat","replies",parentId] cache and get merged
  // into the chronological feed below.
  useRealtimeEvent<MessageDto>(
    "ChatMessageCreated",
    (payload) => {
      if (payload.channelId !== channelId) return;

      // Own-send echo: SendMessageCommandHandler broadcasts via SignalR
      // before flushing the HTTP response, so the echo usually beats
      // composer's onSuccess. Without dedup against the pending temp we'd
      // briefly render BOTH the temp and the real bubble — the "send
      // stutter". Locate the temp by author+body+parent (the only fields we
      // control on the client; no clientId/idempotencyKey echo from server).
      const findOwnTempIndex = (cached: MessageDto[] | undefined) => {
        if (!cached || payload.authorUserId !== selfUserId) return -1;
        return cached.findIndex(
          (m) =>
            m.id.startsWith("temp:") &&
            m.authorUserId === payload.authorUserId &&
            (m.body ?? "") === (payload.body ?? "") &&
            (m.parentMessageId ?? null) === (payload.parentMessageId ?? null),
        );
      };

      if (payload.parentMessageId) {
        // Bump replyCount on the parent — also acts as the trigger that
        // adds this parent to parentIdsWithReplies, which fires a useQueries
        // fetch for the per-parent cache if it isn't already loaded. Drop
        // our own pending temp (Composer onMutate parks reply temps in
        // messagesKey too) — the real DTO lands in the replies cache below.
        queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) => {
          if (!prev) return prev;
          const tempIdx = findOwnTempIndex(prev);
          const base =
            tempIdx >= 0
              ? prev.slice(0, tempIdx).concat(prev.slice(tempIdx + 1))
              : prev;
          return base.map((m) =>
            m.id === payload.parentMessageId
              ? { ...m, replyCount: (m.replyCount ?? 0) + 1 }
              : m,
          );
        });
        // Append to the per-parent cache if it's already loaded; otherwise
        // useQueries' first fetch will pick up the new reply naturally.
        queryClient.setQueryData<MessageDto[] | undefined>(
          ["chat", "replies", payload.parentMessageId],
          (prev) => {
            if (!prev) return prev;
            if (prev.some((m) => m.id === payload.id)) return prev;
            return [...prev, payload];
          },
        );
        return;
      }
      queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) => {
        if (!prev) return [payload];
        if (prev.some((m) => m.id === payload.id)) return prev;
        const tempIdx = findOwnTempIndex(prev);
        if (tempIdx >= 0) {
          const next = prev.slice();
          next[tempIdx] = payload;
          return next;
        }
        return [payload, ...prev];
      });
    },
    [channelId, queryKey, queryClient, selfUserId],
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

  // Pin / unpin broadcast carries the full MessageDto so we just patch
  // by id wherever the message lives (top-level cache or per-parent
  // replies cache) and invalidate the pinned-panel list.
  useRealtimeEvent<MessageDto>(
    "ChatMessagePinned",
    (payload) => {
      if (payload.channelId !== channelId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) =>
        prev?.map((m) => (m.id === payload.id ? payload : m)),
      );
      if (payload.parentMessageId) {
        queryClient.setQueryData<MessageDto[] | undefined>(
          ["chat", "replies", payload.parentMessageId],
          (prev) => prev?.map((m) => (m.id === payload.id ? payload : m)),
        );
      }
      void queryClient.invalidateQueries({ queryKey: ["chat", "pinned", channelId] });
    },
    [channelId, queryKey, queryClient],
  );

  // Read-receipt cross-broadcast: another channel member just advanced their
  // read watermark. Patch the channel cache so ReadReceipt below the caller's
  // latest message recomputes without a refresh.
  useRealtimeEvent<{ channelId: string; userId: string; lastReadMessageId: string }>(
    "ChatChannelMemberRead",
    (payload) => {
      if (payload.channelId !== channelId) return;
      queryClient.setQueryData<import("@/api/chat").ChannelDto | undefined>(
        ["chat", "channel", channelId],
        (prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            members: prev.members.map((m) =>
              m.userId === payload.userId
                ? { ...m, lastReadMessageId: payload.lastReadMessageId }
                : m,
            ),
          };
        },
      );
    },
    [channelId, queryClient],
  );

  useRealtimeEvent<MessageDto>(
    "ChatMessageUnpinned",
    (payload) => {
      if (payload.channelId !== channelId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(queryKey, (prev) =>
        prev?.map((m) => (m.id === payload.id ? payload : m)),
      );
      if (payload.parentMessageId) {
        queryClient.setQueryData<MessageDto[] | undefined>(
          ["chat", "replies", payload.parentMessageId],
          (prev) => prev?.map((m) => (m.id === payload.id ? payload : m)),
        );
      }
      void queryClient.invalidateQueries({ queryKey: ["chat", "pinned", channelId] });
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

  // ── Auto-load all threads (Teams DM/chat style) ────────────────────────
  // Every parent with replyCount > 0 gets its replies fetched in parallel
  // so they appear inline in chronological order with top-level messages.
  // TanStack dedupes; typical channels have <10 active threads.
  const parentIdsWithReplies = useMemo(
    () => messages.filter((m) => (m.replyCount ?? 0) > 0).map((m) => m.id),
    [messages],
  );

  const repliesQueries = useQueries({
    queries: parentIdsWithReplies.map((parentId) => ({
      queryKey: ["chat", "replies", parentId] as const,
      queryFn: () => listMessageReplies(parentId, { pageSize: 100 }),
      staleTime: 30_000,
    })),
  });

  const replyMessages = useMemo<MessageDto[]>(() => {
    const out: MessageDto[] = [];
    for (const q of repliesQueries) {
      if (q.data) out.push(...q.data);
    }
    return out;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [parentIdsWithReplies.length, ...repliesQueries.map((q) => q.data)]);

  // Merge top-level + replies into one chronological list. Each reply's
  // parent context is preserved via the bubble-internal ReplyContextPreview
  // inside the Message component — no need to group here.
  const chronological = useMemo<MessageDto[]>(() => {
    const seen = new Set<string>();
    const merged: MessageDto[] = [];
    for (const m of messages) {
      if (!seen.has(m.id)) {
        seen.add(m.id);
        merged.push(m);
      }
    }
    for (const r of replyMessages) {
      if (!seen.has(r.id)) {
        seen.add(r.id);
        merged.push(r);
      }
    }
    merged.sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc));
    return merged;
  }, [messages, replyMessages]);

  const rows = useMemo<Row[]>(() => {
    const out: Row[] = [];
    let lastDay = "";
    let prevMessage: MessageDto | null = null;

    // Only insert the divider when the watermark is in the loaded window AND
    // there are messages newer than it — otherwise the divider would land on
    // the bottom (nothing unread) or off-screen (all unread, fallback below).
    const watermarkIndex = watermark
      ? chronological.findIndex((m) => m.id === watermark)
      : -1;
    const insertDividerAfter =
      watermarkIndex >= 0 && watermarkIndex < chronological.length - 1
        ? watermarkIndex
        : -1;

    for (let i = 0; i < chronological.length; i++) {
      const m = chronological[i];
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
        prevMessage = null;
      }
    }
    return out;
  }, [chronological, watermark]);

  const parentRef = useRef<HTMLDivElement | null>(null);
  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: (index) => {
      const r = rows[index];
      if (r?.kind === "day") return 32;
      if (r?.kind === "unread") return 36;
      return 72;
    },
    overscan: 8,
  });

  // Jump-to-bottom pill state. Unseen counter only ticks when a new message
  // lands while the user is scrolled away from the bottom.
  const [unseenCount, setUnseenCount] = useState(0);
  const prevLastIdRef = useRef<string | undefined>(undefined);
  const lastMessageId = chronological.at(-1)?.id;

  // Latest own top-level message — used as the anchor for the read receipt
  // below the bubble. We only render the receipt once, on this message, so
  // a long stream of own messages doesn't show a forest of "Seen by N".
  const latestOwnMessageId = useMemo(() => {
    if (!selfUserId) return null;
    for (let i = chronological.length - 1; i >= 0; i--) {
      const m = chronological[i];
      if (m.authorUserId === selfUserId && !m.parentMessageId) return m.id;
    }
    return null;
  }, [chronological, selfUserId]);

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
    // Near top → fetch older. Cooldown is implicit: fetchOlder no-ops while
    // loadingOlder is true, and the scrollTop adjustment after the fetch
    // moves the user away from the trigger zone.
    if (el.scrollTop < 200) void fetchOlder();
  }, [fetchOlder]);

  const jumpToBottom = useCallback(() => {
    if (rows.length === 0) return;
    virtualizer.scrollToIndex(rows.length - 1, { align: "end" });
    setUnseenCount(0);
  }, [virtualizer, rows.length]);

  // ── Jump-to-parent on reply preview click ──────────────────────────────
  // Click a reply's "Replying to {Alice}: ..." block → scroll the feed to
  // the parent and flash it briefly so the user can see the landing spot.
  const [flashingMessageId, setFlashingMessageId] = useState<string | null>(null);
  const flashTimerRef = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (flashTimerRef.current) window.clearTimeout(flashTimerRef.current);
    };
  }, []);

  const jumpToMessage = useCallback(
    (messageId: string): boolean => {
      const index = rows.findIndex(
        (r) => r.kind === "message" && r.message.id === messageId,
      );
      if (index < 0) return false;
      virtualizer.scrollToIndex(index, { align: "center", behavior: "smooth" });
      setFlashingMessageId(messageId);
      if (flashTimerRef.current) window.clearTimeout(flashTimerRef.current);
      flashTimerRef.current = window.setTimeout(
        () => setFlashingMessageId(null),
        1600,
      );
      return true;
    },
    [rows, virtualizer],
  );

  useImperativeHandle(ref, () => ({ jumpToMessage }), [jumpToMessage]);

  if (messagesQuery.isLoading) {
    return (
      <div className="flex h-full items-center justify-center px-6">
        <p className="text-[12px] text-[var(--color-muted-foreground)]">
          Loading messages…
        </p>
      </div>
    );
  }

  if (messages.length === 0) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-2 px-6 text-center">
        <p className="font-display text-[17px] font-semibold tracking-tight text-[var(--color-foreground)]">
          No messages yet
        </p>
        <p className="max-w-sm text-[13px] text-[var(--color-muted-foreground)]">
          This is the very beginning of the conversation. Send the first message to break the silence.
        </p>
      </div>
    );
  }

  return (
    <div className="relative h-full">
      <div
        ref={parentRef}
        onScroll={onScroll}
        className="h-full overflow-y-auto"
        role="log"
        aria-live="polite"
        aria-relevant="additions"
        aria-label="Channel messages"
      >
        {/* aria-hidden so these status rows aren't announced as new messages
            by the role="log" live region (it only relays additions). */}
        {loadingOlder && (
          <div
            aria-hidden
            className="flex h-9 items-center justify-center text-[11px] text-[var(--color-muted-foreground)]"
          >
            Loading older…
          </div>
        )}
        {!hasMoreOlder && messages.length >= 100 && (
          <div
            aria-hidden
            className="flex h-9 items-center justify-center text-[11px] text-[var(--color-muted-foreground)]"
          >
            Beginning of the conversation
          </div>
        )}
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
                    onJumpTo={jumpToMessage}
                    isFlashing={row.message.id === flashingMessageId}
                    members={members}
                    isLatestOwn={row.message.id === latestOwnMessageId}
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
});
