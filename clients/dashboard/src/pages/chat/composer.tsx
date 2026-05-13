import { useEffect, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Send } from "lucide-react";
import { sendMessage } from "@/api/chat";
import { useRealtime } from "@/realtime/realtime-context";
import { cn } from "@/lib/cn";

/**
 * Composer plinth — deliberately styled, brand-tinted on focus. Single-line
 * Enter-to-send; Shift+Enter inserts a newline. Calls Typing(channelId) on
 * each keystroke (the hub itself rate-limits to 1 broadcast per 3s per
 * (channel, user) so we don't need a debounce here).
 */
export function Composer({
  channelId,
  channelTitle,
  parentMessageId,
}: {
  channelId: string;
  channelTitle: string;
  parentMessageId?: string;
}) {
  const [body, setBody] = useState("");
  const [focused, setFocused] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const queryClient = useQueryClient();
  const realtime = useRealtime();

  const mutation = useMutation({
    mutationFn: () =>
      sendMessage({
        channelId,
        body: body.trim(),
        parentMessageId,
        idempotencyKey: crypto.randomUUID(),
      }),
    onMutate: async () => {
      // Optimistic prepend — the realtime broadcast will reconcile when it
      // lands. We use a temp id so the row gets replaced cleanly.
      setBody("");
      requestAnimationFrame(() => textareaRef.current?.focus());
    },
    onSuccess: () => {
      // The realtime ChatMessageCreated event from MessageList already
      // patches the cache; nothing to do here. Invalidate the channel
      // list so the LastMessageAtUtc + sort order refreshes.
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
    },
  });

  // Auto-grow the textarea up to a 6-line cap.
  useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${Math.min(el.scrollHeight, 6 * 24 + 16)}px`;
  }, [body]);

  const onKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      if (body.trim() && !mutation.isPending) mutation.mutate();
    }
  };

  return (
    <div className="px-4 pb-4 pt-2">
      <div
        className={cn(
          "relative rounded-xl border bg-[var(--color-surface-3)] transition-all",
          "duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
          focused
            ? "border-[var(--color-primary)] shadow-[0_0_0_3px_var(--color-primary-soft)]"
            : "border-[var(--color-border)]",
        )}
      >
        <textarea
          ref={textareaRef}
          value={body}
          onChange={(e) => {
            setBody(e.target.value);
            // Fire-and-forget — hub itself throttles per (channel, user).
            void realtime.invoke("Typing", channelId);
          }}
          onFocus={() => setFocused(true)}
          onBlur={() => setFocused(false)}
          onKeyDown={onKeyDown}
          placeholder={
            parentMessageId
              ? "Reply in thread…"
              : `Message ${channelTitle.startsWith("@") ? channelTitle : `#${channelTitle}`}`
          }
          rows={1}
          className={cn(
            "block w-full resize-none border-0 bg-transparent px-4 py-3 pr-14 text-sm",
            "leading-relaxed text-[var(--color-foreground)]",
            "placeholder:text-[var(--color-muted-foreground)]",
            "focus:outline-none",
          )}
        />

        <button
          type="button"
          aria-label="Send message"
          disabled={!body.trim() || mutation.isPending}
          onClick={() => mutation.mutate()}
          className={cn(
            "absolute bottom-2 right-2 grid h-8 w-8 cursor-pointer place-items-center rounded-md",
            "transition-all duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            body.trim() && !mutation.isPending
              ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)] hover:scale-105 hover:bg-[var(--color-primary-hover)]"
              : "bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)]",
          )}
        >
          <Send className="h-3.5 w-3.5" aria-hidden />
        </button>
      </div>

      <div className="mt-1.5 flex items-center justify-between px-1">
        <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          ↵ Send · ⇧↵ Newline · Type @username to mention
        </span>
        {mutation.isError && (
          <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-destructive)]">
            Send failed — retry?
          </span>
        )}
      </div>
    </div>
  );
}
