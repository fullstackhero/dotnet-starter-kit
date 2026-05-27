import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, Pin } from "lucide-react";
import { listPinnedMessages, type MessageDto } from "@/api/chat";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Avatar } from "@/components/ui/avatar";
import { cn } from "@/lib/cn";
import { useUserDisplay } from "@/lib/use-user-display";
import { shortDateTime } from "@/pages/chat/chat-utils";

/**
 * Pinned-messages bar — a slim strip under the channel header that surfaces
 * how many messages are pinned and opens a list to review/jump to them.
 * Renders nothing when the channel has no pins. Replaces the old unlabelled
 * header pin glyph: a labelled, counted bar is far more discoverable.
 * Clicking a row jumps the feed to the message (scroll + flash via the
 * MessageList handle).
 */
export function ChatPinnedBar({
  channelId,
  onJump,
}: {
  channelId: string;
  onJump: (messageId: string) => void;
}) {
  const [open, setOpen] = useState(false);

  // Eager (not gated on `open`) so the bar knows the count and can show or
  // hide itself. Kept fresh by the pin/unpin mutation + realtime handlers,
  // which invalidate this exact key.
  const pinnedQuery = useQuery({
    queryKey: ["chat", "pinned", channelId],
    queryFn: () => listPinnedMessages(channelId),
    staleTime: 60_000,
  });

  const pinned = pinnedQuery.data ?? [];
  // No bar until there's something pinned — keeps the chrome quiet by default.
  if (pinned.length === 0) return null;

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label={`${pinned.length} pinned ${pinned.length === 1 ? "message" : "messages"}, click to review`}
          className={cn(
            "flex w-full shrink-0 cursor-pointer items-center gap-2 px-4 py-1.5 text-[12px]",
            "border-b border-[var(--color-border)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.06)]",
            "text-[var(--color-muted-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] hover:text-[var(--color-foreground)]",
          )}
        >
          <Pin className="h-3.5 w-3.5 shrink-0 text-[var(--color-primary)]" aria-hidden />
          <span className="font-medium">
            {pinned.length} pinned {pinned.length === 1 ? "message" : "messages"}
          </span>
          <ChevronDown
            className={cn(
              "ml-auto h-3.5 w-3.5 transition-transform duration-[var(--duration-fast)]",
              open && "rotate-180",
            )}
            aria-hidden
          />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="start"
        sideOffset={0}
        className="w-[min(420px,calc(100vw-2rem))] p-0"
      >
        <div className="border-b border-[var(--color-border)] px-3 py-2">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Pinned messages
          </p>
        </div>
        <div className="max-h-[60vh] overflow-y-auto">
          <ul className="divide-y divide-[var(--color-border)]">
            {pinned.map((m) => (
              <PinnedRow
                key={m.id}
                message={m}
                onPick={() => {
                  onJump(m.id);
                  setOpen(false);
                }}
              />
            ))}
          </ul>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function PinnedRow({
  message,
  onPick,
}: {
  message: MessageDto;
  onPick: () => void;
}) {
  const author = useUserDisplay(message.authorUserId);
  const preview =
    (message.body ?? "").trim() ||
    (message.attachments.length > 0
      ? `📎 ${message.attachments[0].originalFileName}${
          message.attachments.length > 1 ? ` (+${message.attachments.length - 1})` : ""
        }`
      : "(no text)");
  return (
    <li>
      <button
        type="button"
        onClick={onPick}
        className={cn(
          "flex w-full cursor-pointer items-start gap-2.5 px-3 py-2.5 text-left",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:bg-[var(--color-accent)]",
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
            {preview}
          </p>
        </div>
      </button>
    </li>
  );
}

