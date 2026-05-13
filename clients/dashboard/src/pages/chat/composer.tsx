import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Send, X } from "lucide-react";
import { sendMessage, type ChannelTypeValue, type MessageDto } from "@/api/chat";
import { searchUsers, type UserDto } from "@/api/identity";
import { useRealtime } from "@/realtime/realtime-context";
import { cn } from "@/lib/cn";
import { useUserDisplay } from "@/lib/use-user-display";
import { MentionPicker } from "@/pages/chat/mention-picker";

/**
 * Composer plinth — deliberately styled, brand-tinted on focus. Enter-to-send;
 * Shift+Enter inserts a newline. Calls Typing(channelId) on each keystroke
 * (the hub itself rate-limits to 1 broadcast per 3s per (channel, user) so
 * we don't need a debounce here).
 *
 * @-mentions: when the user types `@` followed by 2+ alphanumeric chars at
 * a word boundary, a floating picker appears above the textarea with
 * matching users from /api/v1/identity/users/search. ↑↓ navigates, Enter
 * selects (inserts `@username ` and closes), Esc dismisses.
 */
export function Composer({
  channelId,
  channelTitle,
  channelType,
  replyTo,
  onClearReply,
}: {
  channelId: string;
  channelTitle: string;
  /** Discriminator for the placeholder: only channels (type=2) get the # prefix. */
  channelType?: ChannelTypeValue;
  /** When set, the composer renders a quoted preview of this message and posts
   *  the next send with parentMessageId = replyTo.id. Teams-DM style. */
  replyTo?: MessageDto | null;
  onClearReply?: () => void;
}) {
  const parentMessageId = replyTo?.id;
  const [body, setBody] = useState("");
  const [focused, setFocused] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const queryClient = useQueryClient();
  const realtime = useRealtime();

  // ── Mention picker state ───────────────────────────────────────────────
  // `mention` is non-null whenever the caret is sitting at the end of an
  // active @-token. `mention.query` is the chars typed after the @. The
  // picker is only visible when query.length >= 2 AND we have candidates.
  const [mention, setMention] = useState<{ query: string; atPos: number } | null>(null);
  const [highlight, setHighlight] = useState(0);
  const [debouncedQuery, setDebouncedQuery] = useState("");

  useEffect(() => {
    if (!mention || mention.query.length < 2) {
      setDebouncedQuery("");
      return;
    }
    const t = setTimeout(() => setDebouncedQuery(mention.query), 200);
    return () => clearTimeout(t);
  }, [mention]);

  const mentionUsersQuery = useQuery({
    queryKey: ["chat", "mention-search", debouncedQuery],
    queryFn: () => searchUsers({ search: debouncedQuery, pageSize: 6, isActive: true }),
    enabled: debouncedQuery.length >= 2,
    staleTime: 30_000,
  });

  const candidates = useMemo<UserDto[]>(
    () => (mentionUsersQuery.data?.items ?? []).filter((u) => !!u.id),
    [mentionUsersQuery.data],
  );

  const pickerOpen =
    mention !== null && mention.query.length >= 2 && candidates.length > 0;

  // Reset highlight whenever the candidate list changes — keeps the
  // selection sensible even as the query narrows.
  useEffect(() => {
    setHighlight(0);
  }, [debouncedQuery, candidates.length]);

  // ── Detection: walk back from the caret to find an active @ token ──────
  // Triggers when the @ is at the start of input or after whitespace
  // (avoids false positives inside emails like "foo@bar.com"). The token
  // body matches the same regex shape the server uses
  // (Modules.Chat MentionParser): [A-Za-z0-9._-]
  const detectMention = (value: string, caretPos: number) => {
    const before = value.slice(0, caretPos);
    const match = before.match(/(?:^|\s)@([A-Za-z0-9._-]*)$/);
    if (!match) {
      setMention(null);
      return;
    }
    setMention({ query: match[1], atPos: caretPos - match[1].length - 1 });
  };

  // ── Send mutation ──────────────────────────────────────────────────────
  // mutate() takes the trimmed text as a variable so the value is captured at
  // call time. If we closed over `body` in mutationFn, onMutate's setBody("")
  // would commit the re-render before TanStack Query reads the latest options
  // ref, and we'd POST an empty body. (Reproduced as a 400 "'Body' must not
  // be empty" before this fix.)
  const mutation = useMutation({
    mutationFn: (text: string) =>
      sendMessage({
        channelId,
        body: text,
        parentMessageId,
        idempotencyKey: crypto.randomUUID(),
      }),
    onMutate: () => {
      // Clear + refocus optimistically. The realtime ChatMessageCreated event
      // from MessageList patches the cache when the broadcast lands.
      setBody("");
      setMention(null);
      onClearReply?.();
      requestAnimationFrame(() => textareaRef.current?.focus());
    },
    onSuccess: () => {
      // Invalidate the channel list so LastMessageAtUtc + sort order refreshes.
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

  const send = () => {
    const trimmed = body.trim();
    if (!trimmed || mutation.isPending) return;
    mutation.mutate(trimmed);
  };

  // Replace the partial @<query> with @<userName> + space.
  const applyMention = (user: UserDto) => {
    if (!mention) return;
    const handle = (user.userName ?? "").trim();
    if (!handle) return; // can't mention someone without a username
    const before = body.slice(0, mention.atPos);
    const after = body.slice(mention.atPos + 1 + mention.query.length);
    const insertion = `@${handle} `;
    const next = before + insertion + after;
    setBody(next);
    setMention(null);
    const newCaret = (before + insertion).length;
    // Re-position the caret after the inserted handle. Needs to happen after
    // React commits the textarea value, so defer to rAF.
    requestAnimationFrame(() => {
      const el = textareaRef.current;
      if (!el) return;
      el.focus();
      el.setSelectionRange(newCaret, newCaret);
    });
  };

  const onKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = (e) => {
    // Picker key handling — short-circuits Enter so a selection doesn't
    // send the message, and consumes Esc so it dismisses the picker
    // instead of blurring the textarea.
    if (pickerOpen) {
      if (e.key === "ArrowDown") {
        e.preventDefault();
        setHighlight((h) => (h + 1) % candidates.length);
        return;
      }
      if (e.key === "ArrowUp") {
        e.preventDefault();
        setHighlight((h) => (h - 1 + candidates.length) % candidates.length);
        return;
      }
      if (e.key === "Enter" || e.key === "Tab") {
        e.preventDefault();
        const choice = candidates[highlight];
        if (choice) applyMention(choice);
        return;
      }
      if (e.key === "Escape") {
        e.preventDefault();
        setMention(null);
        return;
      }
    }

    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      send();
    }

    if (e.key === "Escape" && replyTo) {
      e.preventDefault();
      onClearReply?.();
    }
  };

  return (
    <div className="relative px-4 pb-4 pt-2">
      <div
        className={cn(
          "relative rounded-xl border bg-[var(--color-surface-3)] transition-all",
          "duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
          focused
            ? "border-[var(--color-primary)] shadow-[0_0_0_3px_var(--color-primary-soft)]"
            : "border-[var(--color-border)]",
        )}
      >
        {/* Mention picker floats above the plinth so it never covers the
            text the caller is typing. Positioned within the relative plinth
            container so it tracks the composer's width. */}
        {pickerOpen && (
          <MentionPicker
            candidates={candidates}
            highlight={highlight}
            onHighlight={setHighlight}
            onSelect={applyMention}
            loading={mentionUsersQuery.isLoading}
            className="absolute inset-x-0 bottom-full mb-2"
          />
        )}

        {/* Reply quote preview — Teams DM style. Shows above the textarea
            inside the plinth's border so it shares the focus halo. */}
        {replyTo && <ReplyQuote replyTo={replyTo} onClear={() => onClearReply?.()} />}

        <textarea
          ref={textareaRef}
          value={body}
          onChange={(e) => {
            const next = e.target.value;
            setBody(next);
            detectMention(next, e.target.selectionStart ?? next.length);
            // Fire-and-forget — hub itself throttles per (channel, user).
            void realtime.invoke("Typing", channelId);
          }}
          onSelect={(e) => {
            // Caret moves via arrow keys / mouse click without changing value.
            // Re-check mention context so navigating into a token reopens
            // the picker.
            const el = e.currentTarget;
            detectMention(el.value, el.selectionStart ?? el.value.length);
          }}
          onFocus={() => setFocused(true)}
          onBlur={() => {
            setFocused(false);
            // Close the picker when focus actually leaves the textarea.
            // Mouse-down in the picker preventsDefault so blur doesn't fire
            // for in-picker clicks.
            setMention(null);
          }}
          onKeyDown={onKeyDown}
          placeholder={
            replyTo
              ? "Type your reply…"
              : channelType === 2
                ? `Message #${channelTitle}`
                : `Message ${channelTitle}`
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
          onClick={send}
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
          {replyTo
            ? "↵ Send reply · ⇧↵ Newline · Esc clear"
            : "↵ Send · ⇧↵ Newline · Type @ + 2 chars to mention"}
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

/**
 * Quoted preview of the message being replied to. Sits inside the composer's
 * rounded-xl border above the textarea so it shares the brand-tinted focus
 * halo. Body is truncated to 2 lines; clicking the X dismisses the reply
 * context without losing the in-progress text.
 */
function ReplyQuote({
  replyTo,
  onClear,
}: {
  replyTo: MessageDto;
  onClear: () => void;
}) {
  const author = useUserDisplay(replyTo.authorUserId);
  const body = (replyTo.body ?? "").trim() || "(no text — attachment or empty)";

  return (
    <div
      className={cn(
        "mx-3 mt-3 flex items-start gap-2.5 rounded-md border-l-2 px-3 py-2",
        "border-l-[var(--color-primary)] bg-[var(--color-surface-2)]",
      )}
    >
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5">
          <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            Replying to
          </span>
          <span className="truncate text-[11px] font-semibold tracking-tight text-[var(--color-foreground)]">
            {author.name}
          </span>
        </div>
        <p
          className="mt-0.5 line-clamp-2 text-[12px] leading-snug text-[var(--color-muted-foreground)]"
          title={body}
        >
          {body}
        </p>
      </div>
      <button
        type="button"
        onClick={onClear}
        aria-label="Clear reply context"
        title="Clear reply"
        className={cn(
          "grid h-5 w-5 shrink-0 cursor-pointer place-items-center rounded",
          "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        )}
      >
        <X className="h-3 w-3" aria-hidden />
      </button>
    </div>
  );
}
