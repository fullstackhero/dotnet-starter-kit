import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Search, X } from "lucide-react";
import { searchMessages, type MessageDto } from "@/api/chat";
import { Avatar } from "@/components/ui/avatar";
import { cn } from "@/lib/cn";
import { useUserDisplay } from "@/lib/use-user-display";
import { shortDateTime } from "@/pages/chat/chat-utils";

/**
 * Inline search overlay in the channel header. The icon-button in the
 * channel header opens this; it replaces the header content with a
 * search input and renders a results panel below scoped to the current
 * channel. Click a result → bubble it up via onJump (chat-page asks
 * MessageList to scroll + flash). Esc / back arrow closes.
 */
export function ChatSearchOverlay({
  channelId,
  onClose,
  onJump,
}: {
  channelId: string;
  onClose: () => void;
  onJump: (messageId: string) => void;
}) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const inputRef = useRef<HTMLInputElement | null>(null);

  // Auto-focus the input on mount.
  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  // 250ms debounce — the server FTS query is cheap but no need to fire
  // on every keystroke either.
  useEffect(() => {
    const t = setTimeout(() => setDebounced(query.trim()), 250);
    return () => clearTimeout(t);
  }, [query]);

  // Reset when the channel changes underneath us.
  useEffect(() => {
    setQuery("");
    setDebounced("");
  }, [channelId]);

  const resultsQuery = useQuery({
    queryKey: ["chat", "search", channelId, debounced],
    queryFn: () => searchMessages({ q: debounced, channelId, pageSize: 20 }),
    enabled: debounced.length >= 2,
    staleTime: 30_000,
  });

  const results = resultsQuery.data ?? [];

  const onKeyDown: React.KeyboardEventHandler<HTMLInputElement> = (e) => {
    if (e.key === "Escape") {
      e.preventDefault();
      onClose();
    }
  };

  return (
    <>
      <header className="flex h-14 shrink-0 items-center gap-2 border-b border-[var(--color-border)] px-2">
        <button
          type="button"
          onClick={onClose}
          aria-label="Close search"
          title="Close search"
          className={cn(
            "grid h-8 w-8 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          )}
        >
          <ArrowLeft className="h-4 w-4" />
        </button>
        <label className="relative flex-1" htmlFor="chat-search-input">
          <Search
            aria-hidden
            className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
          />
          <input
            ref={inputRef}
            id="chat-search-input"
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={onKeyDown}
            placeholder="Search messages in this channel"
            spellCheck={false}
            autoComplete="off"
            className={cn(
              "h-9 w-full rounded-lg border bg-[var(--color-card)] pl-8 pr-8 text-sm",
              "border-[var(--color-border)] text-[var(--color-foreground)]",
              "placeholder:text-[var(--color-muted-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus:border-[var(--color-primary)] focus:outline-none",
              "focus:ring-2 focus:ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]",
            )}
          />
          {query.length > 0 && (
            <button
              type="button"
              onClick={() => {
                setQuery("");
                inputRef.current?.focus();
              }}
              aria-label="Clear search"
              className={cn(
                "absolute right-1.5 top-1/2 grid h-6 w-6 -translate-y-1/2 cursor-pointer place-items-center rounded",
                "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              )}
            >
              <X className="h-3 w-3" aria-hidden />
            </button>
          )}
        </label>
      </header>

      {/* Results panel — overlays the MessageList below the header. The
          panel only mounts when the user has typed something so the
          channel feed isn't hidden by an empty dropdown. */}
      {debounced.length >= 2 && (
        <div
          className={cn(
            "absolute left-0 right-0 top-14 z-20 max-h-[60vh] overflow-y-auto",
            "border-b border-[var(--color-border)] bg-[var(--color-card)]",
            "shadow-[0_18px_28px_-18px_oklch(0_0_0_/_0.18)]",
          )}
        >
          {resultsQuery.isLoading ? (
            <ResultPlaceholder label="Searching…" />
          ) : results.length === 0 ? (
            <ResultPlaceholder label={`No matches for "${debounced}".`} />
          ) : (
            <ul className="divide-y divide-[var(--color-border)]">
              {results.map((m) => (
                <SearchResultRow
                  key={m.id}
                  message={m}
                  query={debounced}
                  onPick={() => {
                    onJump(m.id);
                    onClose();
                  }}
                />
              ))}
            </ul>
          )}
          <div className="border-t border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-1.5">
            <span className="text-[11px] text-[var(--color-muted-foreground)]">
              {results.length > 0
                ? `${results.length} match${results.length === 1 ? "" : "es"} · click to jump · Esc to close`
                : "Esc to close"}
            </span>
          </div>
        </div>
      )}
    </>
  );
}

function SearchResultRow({
  message,
  query,
  onPick,
}: {
  message: MessageDto;
  query: string;
  onPick: () => void;
}) {
  const author = useUserDisplay(message.authorUserId);
  const snippet = highlight((message.body ?? "").trim(), query);
  return (
    <li>
      <button
        type="button"
        onClick={onPick}
        className={cn(
          "flex w-full cursor-pointer items-start gap-3 px-3 py-2.5 text-left",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:bg-[var(--color-accent)]",
          "focus-visible:outline-none focus-visible:bg-[var(--color-accent)]",
        )}
      >
        <Avatar
          name={author.name}
          src={author.imageUrl ?? null}
          size="sm"
          className="mt-0.5 shrink-0"
        />
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline gap-1.5">
            <span className="truncate text-[12.5px] font-semibold tracking-tight text-[var(--color-foreground)]">
              {author.name}
            </span>
            <span className="text-[10px] tabular-nums text-[var(--color-muted-foreground)]">
              {shortDateTime(message.createdAtUtc)}
            </span>
          </div>
          <p className="line-clamp-2 text-[12.5px] leading-relaxed text-[var(--color-foreground)]">
            {snippet}
          </p>
        </div>
      </button>
    </li>
  );
}

function ResultPlaceholder({ label }: { label: string; mono?: boolean }) {
  return (
    <div className="px-3 py-8 text-center">
      <p className="text-[13px] text-[var(--color-muted-foreground)]">
        {label}
      </p>
    </div>
  );
}

/**
 * Wraps occurrences of `term` in the body with a brand-tinted <mark> so
 * the user can see WHY the message matched. Case-insensitive, escapes
 * regex metacharacters in the term.
 */
function highlight(body: string, term: string): React.ReactNode {
  if (term.length === 0) return body;
  const escaped = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const parts = body.split(new RegExp(`(${escaped})`, "ig"));
  // split() with a capturing group emits the matched term as the odd-index
  // segments; a case-insensitive equality is enough to pick them out (and
  // avoids a stateful `/g` RegExp.test whose lastIndex drifts across calls).
  return parts.map((part, i) =>
    part.toLowerCase() === term.toLowerCase() ? (
      <mark
        key={i}
        className="rounded-sm bg-[var(--color-primary-soft)] px-0.5 text-[var(--color-primary)]"
      >
        {part}
      </mark>
    ) : (
      <span key={i}>{part}</span>
    ),
  );
}
