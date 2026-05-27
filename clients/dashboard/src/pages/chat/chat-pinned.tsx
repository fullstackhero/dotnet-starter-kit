import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Pin } from "lucide-react";
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
 * Pinned messages dropdown — anchored to a Pin icon button in the channel
 * header. Lists pinned messages most-recently-pinned-first; click a row
 * to jump to the message in the feed (which scrolls + flashes via the
 * MessageList handle).
 */
export function ChatPinnedDropdown({
  channelId,
  onJump,
}: {
  channelId: string;
  onJump: (messageId: string) => void;
}) {
  const [open, setOpen] = useState(false);

  const pinnedQuery = useQuery({
    queryKey: ["chat", "pinned", channelId],
    queryFn: () => listPinnedMessages(channelId),
    enabled: open,
    staleTime: 60_000,
  });

  const pinned = pinnedQuery.data ?? [];

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label="Pinned messages"
          title="Pinned messages"
          className={cn(
            "grid h-8 w-8 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          )}
        >
          <Pin className="h-3.5 w-3.5" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-[320px] p-0">
        <div className="border-b border-[var(--color-border)] px-3 py-2">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Pinned messages
          </p>
        </div>
        <div className="max-h-[60vh] overflow-y-auto">
          {pinnedQuery.isLoading ? (
            <Placeholder label="Loading…" />
          ) : pinned.length === 0 ? (
            <Placeholder label="Nothing pinned in this channel yet." />
          ) : (
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
          )}
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

function Placeholder({ label }: { label: string; mono?: boolean }) {
  return (
    <div className="px-3 py-8 text-center">
      <p className="text-[13px] text-[var(--color-muted-foreground)]">
        {label}
      </p>
    </div>
  );
}
