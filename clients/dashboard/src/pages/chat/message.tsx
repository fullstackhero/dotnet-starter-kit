import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Pencil, SmilePlus, Trash2 } from "lucide-react";
import {
  addReaction,
  deleteMessage,
  editMessage,
  removeReaction,
  type MessageDto,
} from "@/api/chat";
import { Avatar } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/cn";
import { groupReactions, shortenUserId, shortTime } from "@/pages/chat/chat-utils";

const QUICK_REACTIONS = ["👍", "🎉", "❤️", "👀", "🔥", "🚀"] as const;

/**
 * One message row. The visual moves:
 *   - Author has a 12-col avatar gutter on the left so successive messages
 *     from the same person can hide the gutter and merge into a thread block.
 *   - Author of own messages gets a 2px brand strip on the left edge.
 *   - Hover reveals an action rail (react / edit / delete).
 *   - Mention tokens render as inline `chat-mention` pills.
 *   - Reaction chips light up in primary tones for the caller's own reactions.
 */
export function Message({
  message,
  selfUserId,
  isMerged,
  onReply,
}: {
  message: MessageDto;
  selfUserId?: string;
  /** When true, this row continues a thread block — hide the avatar gutter. */
  isMerged: boolean;
  onReply?: (parentMessageId: string) => void;
}) {
  const isOwn = selfUserId === message.authorUserId;
  const isDeleted = message.deletedAtUtc !== null && message.deletedAtUtc !== undefined;
  const reactions = groupReactions(message, selfUserId);

  return (
    <div
      data-mine={isOwn || undefined}
      data-merged={isMerged || undefined}
      className={cn(
        "group/message relative flex gap-3 px-4 py-1",
        // Subtle highlight on hover so the action rail has a backdrop.
        "hover:bg-[oklch(from_var(--color-foreground)_l_c_h_/_0.025)]",
        isMerged ? "pt-0.5" : "pt-2",
      )}
    >
      {/* Own-author hairline — brand-tinted accent on the left edge. */}
      {isOwn && (
        <span
          aria-hidden
          className="absolute left-0 top-0 h-full w-0.5 rounded-r-full bg-[var(--color-primary)] opacity-50"
        />
      )}

      {/* Avatar gutter — shown only on the first message of a block. When
          merged, the gutter holds the time stamp on hover so users can still
          see when each line was posted. */}
      <div className="w-9 shrink-0">
        {isMerged ? (
          <span className="invisible block text-[10px] text-[var(--color-muted-foreground)] group-hover/message:visible">
            {shortTime(message.createdAtUtc)}
          </span>
        ) : (
          <Avatar name={shortenUserId(message.authorUserId)} size="sm" />
        )}
      </div>

      <div className="min-w-0 flex-1">
        {!isMerged && (
          <div className="flex items-baseline gap-2">
            <span className="text-sm font-semibold tracking-tight text-[var(--color-foreground)]">
              {shortenUserId(message.authorUserId)}
            </span>
            <span className="font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">
              {shortTime(message.createdAtUtc)}
            </span>
            {message.editedAtUtc && (
              <span className="font-mono text-[10px] uppercase tracking-[0.10em] text-[var(--color-muted-foreground)]">
                · edited
              </span>
            )}
          </div>
        )}

        {isDeleted ? (
          <span className="text-sm italic text-[var(--color-muted-foreground)]">
            [message deleted]
          </span>
        ) : (
          <MessageBody body={message.body ?? ""} />
        )}

        {/* Reply count chip — small affordance to open the thread. */}
        {message.replyCount > 0 && onReply && (
          <button
            type="button"
            onClick={() => onReply(message.id)}
            className={cn(
              "mt-1 inline-flex h-6 cursor-pointer items-center gap-1 rounded-md border px-1.5",
              "border-[var(--color-border)] bg-[var(--color-surface-2)]",
              "text-[11px] text-[var(--color-muted-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "hover:border-[var(--color-primary)] hover:text-[var(--color-primary)]",
            )}
          >
            <span className="font-mono tabular-nums">{message.replyCount}</span>
            <span>{message.replyCount === 1 ? "reply" : "replies"}</span>
          </button>
        )}

        {/* Reaction row */}
        {reactions.length > 0 && (
          <div className="mt-1.5 flex flex-wrap gap-1">
            {reactions.map((r) => (
              <ReactionChip
                key={r.emoji}
                messageId={message.id}
                emoji={r.emoji}
                count={r.count}
                mine={r.mine}
              />
            ))}
          </div>
        )}
      </div>

      {/* Hover action rail — top-right of the row. */}
      <MessageActions message={message} isOwn={isOwn} onReply={onReply} />
    </div>
  );
}

/**
 * Renders message body with @mention tokens promoted to inline pills. We
 * don't sanitize HTML — the body is rendered as plain text via React text
 * nodes; mentions are matched and split into segments client-side.
 */
function MessageBody({ body }: { body: string }) {
  const segments: Array<{ type: "text" | "mention"; value: string }> = [];
  const re = /(?<!\w)@([A-Za-z0-9._-]+)/g;
  let last = 0;
  let m: RegExpExecArray | null;
  while ((m = re.exec(body)) !== null) {
    if (m.index > last) {
      segments.push({ type: "text", value: body.slice(last, m.index) });
    }
    segments.push({ type: "mention", value: m[1] });
    last = m.index + m[0].length;
  }
  if (last < body.length) {
    segments.push({ type: "text", value: body.slice(last) });
  }
  return (
    <p className="whitespace-pre-wrap text-sm leading-relaxed text-[var(--color-foreground)]">
      {segments.map((seg, i) =>
        seg.type === "mention" ? (
          <span key={i} className="chat-mention">@{seg.value}</span>
        ) : (
          <span key={i}>{seg.value}</span>
        ),
      )}
    </p>
  );
}

function ReactionChip({
  messageId,
  emoji,
  count,
  mine,
}: {
  messageId: string;
  emoji: string;
  count: number;
  mine: boolean;
}) {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => (mine ? removeReaction(messageId, emoji) : addReaction(messageId, emoji)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "messages"] });
    },
  });
  return (
    <button
      type="button"
      data-mine={mine || undefined}
      onClick={() => mutation.mutate()}
      disabled={mutation.isPending}
      className="chat-reaction-chip"
    >
      <span aria-hidden>{emoji}</span>
      <span>{count}</span>
    </button>
  );
}

function MessageActions({
  message,
  isOwn,
  onReply,
}: {
  message: MessageDto;
  isOwn: boolean;
  onReply?: (parentMessageId: string) => void;
}) {
  const [pickerOpen, setPickerOpen] = useState(false);
  const [editing, setEditing] = useState(false);
  const queryClient = useQueryClient();
  const isDeleted = message.deletedAtUtc !== null && message.deletedAtUtc !== undefined;

  const reactMutation = useMutation({
    mutationFn: (emoji: string) => addReaction(message.id, emoji),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "messages"] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteMessage(message.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "messages"] });
    },
  });

  if (isDeleted) return null;

  return (
    <>
      <div
        className={cn(
          "absolute right-3 top-1 z-10 hidden gap-0.5 rounded-md border border-[var(--color-border)]",
          "bg-[var(--color-popover)] p-0.5 shadow-[var(--shadow-md)]",
          "group-hover/message:flex",
        )}
      >
        <ActionButton title="React" onClick={() => setPickerOpen((v) => !v)}>
          <SmilePlus className="h-3.5 w-3.5" />
        </ActionButton>
        {!message.parentMessageId && onReply && (
          <ActionButton title="Reply in thread" onClick={() => onReply(message.id)}>
            <span className="font-mono text-[10px] font-semibold tracking-tight">↪</span>
          </ActionButton>
        )}
        {isOwn && (
          <ActionButton title="Edit" onClick={() => setEditing(true)}>
            <Pencil className="h-3.5 w-3.5" />
          </ActionButton>
        )}
        {isOwn && (
          <ActionButton
            title="Delete"
            onClick={() => deleteMutation.mutate()}
            destructive
          >
            <Trash2 className="h-3.5 w-3.5" />
          </ActionButton>
        )}
      </div>

      {pickerOpen && (
        <div
          className={cn(
            "absolute right-3 top-9 z-20 flex gap-1 rounded-md border border-[var(--color-border)]",
            "bg-[var(--color-popover)] p-1 shadow-[var(--shadow-lg)]",
          )}
        >
          {QUICK_REACTIONS.map((emoji) => (
            <button
              key={emoji}
              type="button"
              onClick={() => {
                reactMutation.mutate(emoji);
                setPickerOpen(false);
              }}
              className={cn(
                "grid h-7 w-7 cursor-pointer place-items-center rounded-md text-base",
                "hover:bg-[var(--color-accent)]",
                "transition-transform duration-[var(--duration-fast)] hover:scale-110",
              )}
            >
              {emoji}
            </button>
          ))}
        </div>
      )}

      {editing && (
        <EditMessageInline
          message={message}
          onClose={() => setEditing(false)}
          onSaved={() => {
            void queryClient.invalidateQueries({ queryKey: ["chat", "messages"] });
            setEditing(false);
          }}
        />
      )}
    </>
  );
}

function ActionButton({
  title,
  onClick,
  destructive,
  children,
}: {
  title: string;
  onClick: () => void;
  destructive?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      title={title}
      aria-label={title}
      onClick={onClick}
      className={cn(
        "grid h-6 w-6 cursor-pointer place-items-center rounded",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        destructive
          ? "text-[var(--color-destructive)] hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)]"
          : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
      )}
    >
      {children}
    </button>
  );
}

function EditMessageInline({
  message,
  onClose,
  onSaved,
}: {
  message: MessageDto;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [body, setBody] = useState(message.body ?? "");
  const mutation = useMutation({
    mutationFn: () => editMessage(message.id, body.trim()),
    onSuccess: onSaved,
  });
  return (
    <div className="ml-12 mt-1.5 flex items-center gap-1.5">
      <Input
        value={body}
        onChange={(e) => setBody(e.target.value)}
        autoFocus
        onKeyDown={(e) => {
          if (e.key === "Escape") onClose();
          if (e.key === "Enter" && body.trim()) {
            e.preventDefault();
            mutation.mutate();
          }
        }}
      />
      <Button size="sm" disabled={!body.trim()} onClick={() => mutation.mutate()}>
        Save
      </Button>
      <Button size="sm" variant="ghost" onClick={onClose}>
        Cancel
      </Button>
    </div>
  );
}
