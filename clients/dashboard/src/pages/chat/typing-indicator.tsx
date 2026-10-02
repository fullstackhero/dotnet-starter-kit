import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { useUserDisplay } from "@/lib/use-user-display";

/** Auto-clear typing markers after 4s — slightly longer than the hub's 3s throttle. */
const TYPING_TTL_MS = 4_000;

type Marker = { userId: string; expiresAt: number };

/**
 * Typing presence row — displayed in the gap between the message list and
 * the composer. Three pulsing dots + a "{user} is typing…" mono caption.
 * Multiple typers collapse to "{a}, {b} are typing…" / "{a} and 2 others".
 */
export function TypingIndicator({
  channelId,
  selfUserId,
}: {
  channelId: string;
  selfUserId?: string;
}) {
  const { t } = useTranslation("chat");
  const [markers, setMarkers] = useState<Marker[]>([]);

  useRealtimeEvent<{ channelId: string; userId: string }>(
    "ChatTypingStarted",
    (payload) => {
      if (payload.channelId !== channelId) return;
      if (payload.userId === selfUserId) return;
      setMarkers((prev) => {
        const now = Date.now();
        const filtered = prev.filter((m) => m.userId !== payload.userId && m.expiresAt > now);
        return [...filtered, { userId: payload.userId, expiresAt: now + TYPING_TTL_MS }];
      });
    },
    [channelId, selfUserId],
  );

  // Sweep expired markers each second.
  useEffect(() => {
    if (markers.length === 0) return;
    const tick = setInterval(() => {
      const now = Date.now();
      setMarkers((prev) => {
        const next = prev.filter((m) => m.expiresAt > now);
        return next.length === prev.length ? prev : next;
      });
    }, 1_000);
    return () => clearInterval(tick);
  }, [markers.length]);

  // Resolve up to two names via fixed hook calls (hooks can't run inside a
  // .map). Ids may be undefined when fewer markers are active — the hook
  // tolerates that and the phrase below only reads the names it needs.
  const name0 = useUserDisplay(markers[0]?.userId).name;
  const name1 = useUserDisplay(markers[1]?.userId).name;

  if (markers.length === 0) {
    // Reserve the line height so the composer doesn't jump when typing starts.
    return <div className="h-5 px-4" aria-hidden />;
  }

  const phrase =
    markers.length === 1
      ? t("typing.one", { name: name0 })
      : markers.length === 2
        ? t("typing.two", { a: name0, b: name1 })
        : t("typing.many", { a: name0, count: markers.length - 1 });

  return (
    <div
      className="flex h-5 items-center gap-2 px-4 text-[11px] text-[var(--color-muted-foreground)]"
      role="status"
      aria-live="polite"
      aria-atomic="true"
    >
      <span aria-hidden className="inline-flex items-center gap-0.5">
        <span className="chat-typing-dot inline-block h-1 w-1 rounded-full bg-[var(--color-primary)]" />
        <span className="chat-typing-dot inline-block h-1 w-1 rounded-full bg-[var(--color-primary)]" />
        <span className="chat-typing-dot inline-block h-1 w-1 rounded-full bg-[var(--color-primary)]" />
      </span>
      <span>{phrase}</span>
    </div>
  );
}
