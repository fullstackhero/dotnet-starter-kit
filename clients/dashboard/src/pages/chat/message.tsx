import { forwardRef, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Download, Eye, Paperclip, Pencil, Pin, PinOff, SmilePlus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  addReaction,
  deleteMessage,
  editMessage,
  findOrCreateDm,
  pinMessage,
  removeReaction,
  unpinMessage,
  type ChannelMemberDto,
  type MessageAttachmentDto,
  type MessageDto,
} from "@/api/chat";
import { formatBytes } from "@/hooks/use-file-upload";
import { useAuth } from "@/auth/use-auth";
import { Avatar } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";
import { useUserByUsername, useUserDisplay } from "@/lib/use-user-display";
import { usePresence } from "@/realtime/use-presence";
import { groupReactions, shortTime } from "@/pages/chat/chat-utils";

const QUICK_REACTIONS = ["👍", "🎉", "❤️", "👀", "🔥", "🚀"] as const;

/**
 * One message row, Teams-style:
 *   - Own messages: no avatar, right-aligned brand-tinted bubble, time below.
 *   - Other messages: avatar on the left, surface-tinted bubble on the right.
 *   - Reply messages render a quoted preview of the parent inside the bubble
 *     so scrolling history still tells you who-said-what.
 *   - Hover reveals an action rail floating above the bubble.
 *   - Mention tokens render as inline `chat-mention` pills (with profile peek).
 *   - Reaction chips light up in primary tones for the caller's own reactions.
 */
export function Message({
  message,
  selfUserId,
  isMerged,
  onReply,
  onJumpTo,
  isFlashing,
  members,
  isLatestOwn,
}: {
  message: MessageDto;
  selfUserId?: string;
  /** When true, this row continues a block from the same author — hide the
   *  author header (and avatar on the other-side branch). */
  isMerged: boolean;
  /** Called when the user clicks "Reply" on the hover rail. The composer
   *  in chat-page reacts by entering reply mode with a quoted preview. */
  onReply?: (parent: MessageDto) => void;
  /** Called when the user clicks the reply-context preview at the top of a
   *  reply bubble. MessageList scrolls to and flashes the parent. */
  onJumpTo?: (messageId: string) => void;
  /** Briefly true after another row's reply preview jumps here; drives the
   *  brand-tinted ring + fade. */
  isFlashing?: boolean;
  /** Channel members — used by ReadReceipt below the latest own message. */
  members?: ChannelMemberDto[];
  /** Anchors the read receipt to this message (the latest own top-level). */
  isLatestOwn?: boolean;
}) {
  const isOwn = selfUserId === message.authorUserId;
  const isDeleted = message.deletedAtUtc !== null && message.deletedAtUtc !== undefined;
  const isPending = message.id.startsWith("temp:");
  const reactions = groupReactions(message, selfUserId);
  const author = useUserDisplay(message.authorUserId);
  // Only call usePresence for the other-side branch — own messages don't
  // need a presence dot on their own avatar (and we hide it anyway).
  const authorOnline = usePresence(!isOwn ? message.authorUserId : null);
  const queryClient = useQueryClient();

  // Look up the parent message from the channel's top-level cache so the
  // bubble can render a quoted preview ("Replying to Alice: hello team").
  // Non-reactive: we let the parent component re-render this when the
  // upstream cache changes (which MessageList already watches).
  const parent = useMemo<MessageDto | null>(() => {
    if (!message.parentMessageId) return null;
    const cached = queryClient.getQueryData<MessageDto[]>([
      "chat",
      "messages",
      message.channelId,
    ]);
    return cached?.find((m) => m.id === message.parentMessageId) ?? null;
  }, [queryClient, message.parentMessageId, message.channelId]);

  return (
    <div
      data-mine={isOwn || undefined}
      data-merged={isMerged || undefined}
      data-pending={isPending || undefined}
      className={cn(
        "group/message relative flex gap-2 px-4 pt-2 pb-1",
        isOwn ? "justify-end" : "justify-start",
        // Uniform row padding regardless of merge state — every bubble is
        // separated from the previous one by the same 12px (next pt-2 +
        // previous pb-1 = 12). When the row is non-merged, the author
        // header that renders inside the inner column adds its own visual
        // break ABOVE the bubble; the row's outer padding stays constant.
        // Earlier conditional pt-3.5 / pt-6 was the source of the "weird,
        // non-uniform" spacing — every transition (merged↔merged,
        // merged↔non-merged) produced a slightly different gap.
        // Tentative own messages fade until the realtime echo lands.
        isPending && "opacity-70",
      )}
    >
      {/* Avatar gutter — only on the other-side branch and only on the
          first-of-block row. Merged rows just hold an empty 9-col slot so
          the bubble lines up under the previous one. */}
      {!isOwn && (
        <div className="w-9 shrink-0 self-end">
          {!isMerged && (
            <Avatar
              name={author.name}
              src={author.imageUrl ?? null}
              size="sm"
              status={authorOnline ? "online" : "offline"}
            />
          )}
        </div>
      )}

      {/* Hover-only timestamp on the side opposite the avatar — visible
          only while the row is hovered, so merged blocks stay quiet by
          default and the time is still discoverable on demand. */}
      {isMerged && (
        <span
          aria-hidden
          className={cn(
            "pointer-events-none absolute top-1.5 hidden text-[10px] tabular-nums",
            "text-[var(--color-muted-foreground)] group-hover/message:block",
            isOwn ? "left-4" : "right-4",
          )}
        >
          {shortTime(message.createdAtUtc)}
        </span>
      )}

      <div
        className={cn(
          "relative flex min-w-0 max-w-[78%] flex-col",
          isOwn ? "items-end" : "items-start",
        )}
      >
        {/* Header row — name + handle + time for others, just time for own
            (the user knows who they are). Shown only on first-of-block;
            merged rows pick up the time via the hover-only label above.
            Sits as its own flex item ABOVE the bubble inside the inner
            column; outer row padding is uniform so the author-header's
            visual break is the SAME between every author-change boundary. */}
        {!isMerged && (
          <div
            className={cn(
              "mb-1 flex items-baseline gap-2 px-1",
              isOwn && "justify-end",
            )}
          >
            {!isOwn && (
              <>
                <span
                  className="text-[12px] font-semibold tracking-tight text-[var(--color-foreground)]"
                  title={author.handle ? `@${author.handle}` : undefined}
                >
                  {author.name}
                </span>
                {author.handle && author.handle.toLowerCase() !== author.name.toLowerCase() && (
                  <span className="text-[10.5px] text-[var(--color-muted-foreground)]">
                    @{author.handle}
                  </span>
                )}
              </>
            )}
            <span className="text-[10px] tabular-nums text-[var(--color-muted-foreground)]">
              {shortTime(message.createdAtUtc)}
            </span>
            {message.editedAtUtc && (
              <span className="text-[10px] text-[var(--color-muted-foreground)]">
                · edited
              </span>
            )}
          </div>
        )}

        {/* Pinned indicator — above the bubble. */}
        {message.isPinned && (
          <span
            className={cn(
              "mb-0.5 inline-flex items-center gap-1 self-end px-1",
              "text-[10px] font-semibold uppercase tracking-wider",
              "text-[var(--color-primary)]",
            )}
            aria-label="Pinned"
          >
            <Pin className="h-2.5 w-2.5" aria-hidden />
            Pinned
          </span>
        )}

        {/* The bubble. Crisp styling — no transitions or layered shadows that
            would bleed into a halo around the rounded fill (consecutive
            bubbles previously read as a fused glow because shadow interpolation
            mid-flash was leaving ghost rings). */}
        <div
          className={cn(
            "break-words rounded-2xl px-3.5 py-2 shadow-xs",
            isOwn
              ? "bg-[var(--color-primary-soft)] text-[var(--color-foreground)] rounded-tr-md"
              : "border border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-foreground)] rounded-tl-md",
            isFlashing && "ring-2 ring-[var(--color-primary)]",
            message.isPinned &&
              !isFlashing &&
              "ring-1 ring-[oklch(from_var(--color-primary)_l_c_h_/_0.35)]",
          )}
        >
          {/* Reply context preview — quotes the parent so the reader can tell
              what the author replied to without scrolling. Clickable: jumps
              the feed to the parent message. */}
          {message.parentMessageId && (
            <ReplyContextPreview
              parent={parent}
              onClick={
                onJumpTo
                  ? () => onJumpTo(message.parentMessageId!)
                  : undefined
              }
            />
          )}

          {isDeleted ? (
            <span className="text-sm italic text-[var(--color-muted-foreground)]">
              [message deleted]
            </span>
          ) : (
            <>
              {message.body && message.body.length > 0 && (
                <MessageBody body={message.body} />
              )}
              {message.attachments.length > 0 && (
                <MessageAttachments
                  attachments={message.attachments}
                  hasBody={Boolean(message.body && message.body.length > 0)}
                />
              )}
            </>
          )}
        </div>

        {/* Hover action rail — anchored to the bubble's column so it tucks
            into the margin right beside the bubble (left of own messages,
            right of others) instead of drifting to the far pane edge. */}
        <MessageActions message={message} isOwn={isOwn} onReply={onReply} />

        {/* Reactions — align toward the bubble's outer edge. */}
        {reactions.length > 0 && (
          <div
            className={cn(
              "mt-1 flex flex-wrap gap-1 px-1",
              isOwn ? "justify-end" : "justify-start",
            )}
          >
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

        {/* Read receipt — anchored to the caller's latest top-level message. */}
        {isOwn && isLatestOwn && members && (
          <ReadReceipt
            messageId={message.id}
            members={members}
            selfUserId={selfUserId}
          />
        )}
      </div>
    </div>
  );
}

/**
 * Inline quoted preview of the parent message, rendered inside the reply's
 * own bubble. Allows readers to identify the context without clicking into
 * a thread surface. When `onClick` is provided, the preview becomes a
 * button that jumps the feed to the parent.
 */
function ReplyContextPreview({
  parent,
  onClick,
}: {
  parent: MessageDto | null;
  onClick?: () => void;
}) {
  const author = useUserDisplay(parent?.authorUserId);
  const body = parent ? (parent.body ?? "").trim() : "";
  const preview = body || "(no text — attachment or empty)";

  const className = cn(
    "mb-1.5 -mt-0.5 block w-full border-l-2 pl-2 text-left",
    "border-l-[var(--color-primary)]",
    onClick && [
      "cursor-pointer rounded-sm transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
      "hover:bg-[oklch(from_var(--color-foreground)_l_c_h_/_0.04)]",
    ],
  );

  const content = (
    <>
      <div className="flex items-center gap-1.5">
        <span className="text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          Replying to
        </span>
        <span className="truncate text-[11px] font-semibold tracking-tight text-[var(--color-foreground)]">
          {parent ? author.name : "a message"}
        </span>
      </div>
      <p
        className="line-clamp-1 text-[11px] leading-snug text-[var(--color-muted-foreground)]"
        title={preview}
      >
        {preview}
      </p>
    </>
  );

  return onClick ? (
    <button type="button" onClick={onClick} className={className}>
      {content}
    </button>
  ) : (
    <div className={className}>{content}</div>
  );
}



/**
 * Renders message body with @mention tokens promoted to inline pills. We
 * don't sanitize HTML — the body is rendered as plain text via React text
 * nodes; mentions are matched and split into segments client-side. Mention
 * pills are interactive — clicking one copies "@username" so it's easy to
 * tag the same person back in a reply. Profile-peek is a future iteration.
 */
/**
 * "Seen by" caption under the caller's latest own top-level message.
 * Filters channel members to those whose lastReadMessageId watermark is
 * at or past this message id (Guid v7 sorts lexically by time, so a
 * string compare gives a chronological compare). In 1-on-1 DMs collapses
 * to a simple "Seen"; in larger channels it shows "Seen by N".
 */
function ReadReceipt({
  messageId,
  members,
  selfUserId,
}: {
  messageId: string;
  members: ChannelMemberDto[];
  selfUserId?: string;
}) {
  const readers = useMemo(
    () =>
      members.filter(
        (m) =>
          m.userId !== selfUserId &&
          !!m.lastReadMessageId &&
          m.lastReadMessageId >= messageId,
      ),
    [members, selfUserId, messageId],
  );
  if (readers.length === 0) return null;
  const totalOthers = members.filter((m) => m.userId !== selfUserId).length;
  const label =
    totalOthers === 1
      ? "Seen"
      : readers.length === totalOthers
        ? "Seen by everyone"
        : `Seen by ${readers.length}`;
  return (
    <span
      className={cn(
        "mt-0.5 flex items-center gap-1 self-end px-1",
        "text-[10px] tabular-nums text-[var(--color-muted-foreground)]",
      )}
      aria-label={label}
    >
      <Eye className="h-2.5 w-2.5" aria-hidden />
      {label}
    </span>
  );
}

/**
 * Renders message attachments inside the bubble. Images become small inline
 * tiles (clickable to open the underlying signed URL in a new tab); other
 * file types render as a chip with a paperclip icon, file name, size, and
 * a download glyph.
 */
function MessageAttachments({
  attachments,
  hasBody,
}: {
  attachments: MessageAttachmentDto[];
  hasBody: boolean;
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", hasBody && "mt-2")}>
      {attachments.map((a) => (
        <AttachmentTile key={a.id} attachment={a} />
      ))}
    </div>
  );
}

function AttachmentTile({ attachment }: { attachment: MessageAttachmentDto }) {
  const isImage = attachment.contentType.startsWith("image/");
  const canOpen = Boolean(attachment.url);

  if (isImage && attachment.url) {
    return (
      <a
        href={attachment.url}
        target="_blank"
        rel="noreferrer noopener"
        className={cn(
          "group/att relative block max-w-[280px] overflow-hidden rounded-lg",
          "ring-1 ring-[var(--color-border)]",
          "transition-shadow duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:ring-[var(--color-primary)]",
        )}
        title={attachment.originalFileName}
      >
        <img
          src={attachment.url}
          alt={attachment.originalFileName}
          className="block h-auto max-h-[260px] w-full object-cover"
          loading="lazy"
        />
        <span
          aria-hidden
          className={cn(
            "pointer-events-none absolute inset-x-0 bottom-0 flex items-center justify-between gap-2 px-2.5 py-1.5",
            "bg-gradient-to-t from-[oklch(0_0_0_/_0.55)] to-transparent",
            "text-[11px] font-medium text-[var(--color-overlay-foreground)]",
            "opacity-0 transition-opacity duration-[var(--duration-fast)] group-hover/att:opacity-100",
          )}
        >
          <span className="truncate">{attachment.originalFileName}</span>
          <span className="tabular-nums">{formatBytes(attachment.sizeBytes)}</span>
        </span>
      </a>
    );
  }

  return (
    <a
      href={canOpen ? attachment.url : undefined}
      target="_blank"
      rel="noreferrer noopener"
      aria-disabled={!canOpen}
      className={cn(
        "flex items-center gap-2.5 rounded-lg border px-3 py-2 max-w-[320px]",
        "border-[var(--color-border)] bg-[var(--color-card)]",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        canOpen && "hover:border-[var(--color-primary)] hover:bg-[var(--color-muted)]",
        !canOpen && "pointer-events-none opacity-70",
      )}
      title={attachment.originalFileName}
    >
      <span
        aria-hidden
        className={cn(
          "grid h-9 w-9 shrink-0 place-items-center rounded-lg",
          "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        )}
      >
        <Paperclip className="h-4 w-4" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate text-[12.5px] font-medium text-[var(--color-foreground)]">
          {attachment.originalFileName}
        </p>
        <p className="text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {formatBytes(attachment.sizeBytes)}
        </p>
      </div>
      {canOpen && (
        <Download
          className="h-3.5 w-3.5 shrink-0 text-[var(--color-muted-foreground)]"
          aria-hidden
        />
      )}
    </a>
  );
}

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
        seg.type === "mention" ? <MentionPill key={i} username={seg.value} /> : <span key={i}>{seg.value}</span>,
      )}
    </p>
  );
}

function MentionPill({ username }: { username: string }) {
  const [open, setOpen] = useState(false);
  const { user } = useAuth();
  const { resolved, loading, error } = useUserByUsername(username, open);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const isSelf = !!resolved?.id && resolved.id === user?.id;

  const copy = () => {
    const text = `@${username}`;
    void navigator.clipboard
      ?.writeText(text)
      .then(() => toast.success(`Copied ${text}`))
      .catch(() => toast.error("Couldn't copy to clipboard"));
  };

  const dmMutation = useMutation({
    mutationFn: () => {
      if (!resolved?.id) throw new Error("Cannot DM an unresolved user");
      return findOrCreateDm([resolved.id]);
    },
    onSuccess: (channelId) => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      setOpen(false);
      navigate(`/chat/${channelId}`);
    },
    onError: (err) => toast.error("Couldn't open DM", { description: describe(err) }),
  });

  const displayName =
    [resolved?.firstName, resolved?.lastName].filter(Boolean).join(" ").trim() ||
    resolved?.userName ||
    resolved?.email ||
    `@${username}`;

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className="chat-mention"
          title={`Mention of ${username}`}
          aria-label={`Mention of ${username}, click to open profile`}
        >
          @{username}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-[280px] p-0">
        <div className="flex items-start gap-3 p-3">
          <Avatar
            name={displayName}
            src={resolved?.imageUrl ?? null}
            size="md"
            className="shrink-0"
          />
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-1.5">
              <span className="truncate text-sm font-semibold tracking-tight text-[var(--color-foreground)]">
                {loading ? `@${username}` : displayName}
              </span>
              {isSelf && (
                <span className="rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  you
                </span>
              )}
            </div>
            <div className="truncate text-[11px] text-[var(--color-muted-foreground)]">
              @{resolved?.userName ?? username}
            </div>
            {resolved?.email && (
              <div className="truncate text-[11px] text-[var(--color-muted-foreground)]">
                {resolved.email}
              </div>
            )}
            {loading && (
              <div className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">
                Looking up…
              </div>
            )}
            {!loading && !resolved && !error && (
              <div className="mt-1 text-xs italic text-[var(--color-muted-foreground)]">
                User not found.
              </div>
            )}
            {error && (
              <div className="mt-1 text-xs italic text-[var(--color-destructive)]">
                Couldn&apos;t load this user.
              </div>
            )}
          </div>
        </div>
        <div className="flex items-center justify-end gap-1.5 border-t border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2">
          <Button
            size="sm"
            variant="ghost"
            onClick={copy}
            disabled={dmMutation.isPending}
          >
            Copy @{resolved?.userName ?? username}
          </Button>
          <Button
            size="sm"
            disabled={!resolved?.id || isSelf || dmMutation.isPending}
            onClick={() => dmMutation.mutate()}
            title={isSelf ? "That's you" : undefined}
          >
            {dmMutation.isPending ? "Opening…" : "Open DM"}
          </Button>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
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
      aria-pressed={mine}
      aria-label={`${mine ? "Remove your" : "Add"} ${emoji} reaction, ${count} so far`}
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
  onReply?: (parent: MessageDto) => void;
}) {
  const [pickerOpen, setPickerOpen] = useState(false);
  const [editing, setEditing] = useState(false);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
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
      setConfirmingDelete(false);
      toast.success("Message deleted");
    },
    onError: (err) => toast.error("Couldn't delete the message", { description: describe(err) }),
  });

  const pinMutation = useMutation({
    mutationFn: () =>
      message.isPinned ? unpinMessage(message.id) : pinMessage(message.id),
    onSuccess: () => {
      // Realtime ChatMessagePinned / Unpinned will patch the cache too —
      // invalidate as a defensive belt to cover initial loads + the
      // pinned-panel cache.
      void queryClient.invalidateQueries({ queryKey: ["chat", "messages"] });
      void queryClient.invalidateQueries({ queryKey: ["chat", "pinned", message.channelId] });
      toast.success(message.isPinned ? "Unpinned" : "Pinned");
    },
    onError: (err) => toast.error("Couldn't update the pin", { description: describe(err) }),
  });

  if (isDeleted) return null;

  return (
    <>
      <div
        className={cn(
          "absolute top-0 z-10 hidden gap-0.5 rounded-lg border border-[var(--color-border)]",
          "bg-[var(--color-popover)] p-0.5 shadow-[var(--shadow-md)]",
          // Keep the rail mounted while its emoji picker is open so the
          // portaled picker stays anchored to a visible trigger.
          pickerOpen ? "flex" : "group-hover/message:flex",
          // Float in the margin immediately beside the bubble (anchored to the
          // bubble's column, not the pane): own messages sit right so the rail
          // tucks just left of the bubble; others sit left so it tucks right.
          isOwn ? "right-full mr-1.5" : "left-full ml-1.5",
        )}
      >
        {/* React — a portaled menu so the picker can't be clipped or
            click-blocked by the next virtualized message row (the old
            absolutely-positioned picker overflowed its row and the row
            below stole the clicks). */}
        <DropdownMenu open={pickerOpen} onOpenChange={setPickerOpen}>
          <DropdownMenuTrigger asChild>
            <ActionButton title="React">
              <SmilePlus className="h-3.5 w-3.5" />
            </ActionButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            side="top"
            align={isOwn ? "end" : "start"}
            className="flex w-auto min-w-0 gap-1 p-1"
          >
            {QUICK_REACTIONS.map((emoji) => (
              <button
                key={emoji}
                type="button"
                aria-label={`React with ${emoji}`}
                onClick={() => {
                  reactMutation.mutate(emoji);
                  setPickerOpen(false);
                }}
                className={cn(
                  "grid h-8 w-8 cursor-pointer place-items-center rounded-md text-base",
                  "hover:bg-[var(--color-accent)]",
                  "transition-transform duration-[var(--duration-fast)] hover:scale-110",
                )}
              >
                {emoji}
              </button>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        {!message.parentMessageId && onReply && (
          <ActionButton title="Reply" onClick={() => onReply(message)}>
            <span className="text-[12px] font-semibold leading-none">↪</span>
          </ActionButton>
        )}
        <ActionButton
          title={message.isPinned ? "Unpin" : "Pin"}
          onClick={() => pinMutation.mutate()}
        >
          {message.isPinned ? (
            <PinOff className="h-3.5 w-3.5" />
          ) : (
            <Pin className="h-3.5 w-3.5" />
          )}
        </ActionButton>
        {isOwn && (
          <ActionButton title="Edit" onClick={() => setEditing(true)}>
            <Pencil className="h-3.5 w-3.5" />
          </ActionButton>
        )}
        {isOwn && (
          <ActionButton
            title="Delete"
            onClick={() => setConfirmingDelete(true)}
            destructive
          >
            <Trash2 className="h-3.5 w-3.5" />
          </ActionButton>
        )}
      </div>

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

      <DeleteMessageDialog
        message={message}
        open={confirmingDelete}
        onOpenChange={setConfirmingDelete}
        onConfirm={() => deleteMutation.mutate()}
        pending={deleteMutation.isPending}
      />
    </>
  );
}

function DeleteMessageDialog({
  message,
  open,
  onOpenChange,
  onConfirm,
  pending,
}: {
  message: MessageDto;
  open: boolean;
  onOpenChange: (v: boolean) => void;
  onConfirm: () => void;
  pending: boolean;
}) {
  const preview = (message.body ?? "").trim();
  const display = preview.length > 140 ? `${preview.slice(0, 140)}…` : preview;
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Delete this message?</DialogTitle>
          <DialogDescription>
            This can&apos;t be undone. Everyone in the channel will see the
            message disappear in real time.
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          {display ? (
            <blockquote
              className={cn(
                "rounded-lg border-l-2 border-[var(--color-primary)] bg-[var(--color-muted)]",
                "px-3 py-2 text-sm italic text-[var(--color-muted-foreground)]",
              )}
            >
              {display}
            </blockquote>
          ) : (
            <p className="text-[12px] italic text-[var(--color-muted-foreground)]">
              No text preview — attachments or empty body.
            </p>
          )}
        </DialogBody>
        <DialogFooter>
          <Button variant="outline" size="sm" onClick={() => onOpenChange(false)} disabled={pending}>
            Cancel
          </Button>
          <Button variant="destructive" size="sm" onClick={onConfirm} disabled={pending}>
            {pending ? "Deleting…" : "Delete message"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// forwardRef + prop spread so this can serve as a Radix `asChild` trigger
// (the React button is a DropdownMenuTrigger) — Radix injects ref, onClick,
// and aria/data-state props that must reach the underlying <button>.
const ActionButton = forwardRef<
  HTMLButtonElement,
  React.ComponentPropsWithoutRef<"button"> & { destructive?: boolean }
>(function ActionButton({ title, destructive, children, className, ...rest }, ref) {
  return (
    <button
      ref={ref}
      title={title}
      aria-label={title}
      className={cn(
        "grid h-7 w-7 cursor-pointer place-items-center rounded-md",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        destructive
          ? "text-[var(--color-destructive)] hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)]"
          : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        className,
      )}
      {...rest}
      type="button"
    >
      {children}
    </button>
  );
});

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
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const mutation = useMutation({
    mutationFn: () => editMessage(message.id, body.trim()),
    onSuccess: onSaved,
  });

  // Auto-grow up to 6 lines — mirrors the composer's behaviour so multi-line
  // bodies render at their natural height instead of being truncated to one row.
  useLayoutEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${Math.min(el.scrollHeight, 6 * 24 + 16)}px`;
  }, [body]);

  // Park the caret at the end of the prefilled body on mount so users can
  // continue typing without re-positioning their cursor.
  useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    const len = el.value.length;
    el.setSelectionRange(len, len);
  }, []);

  const onKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = (e) => {
    if (e.key === "Escape") {
      e.preventDefault();
      onClose();
      return;
    }
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      if (body.trim() && !mutation.isPending) mutation.mutate();
    }
  };

  return (
    <div className="mt-1.5 w-full space-y-1.5">
      <div
        className={cn(
          "relative rounded-xl border bg-[var(--color-card)] transition-all",
          "duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
          "border-[var(--color-border)] focus-within:border-[var(--color-primary)]",
          "focus-within:shadow-[0_0_0_3px_var(--color-primary-soft)]",
        )}
      >
        <textarea
          ref={textareaRef}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          onKeyDown={onKeyDown}
          autoFocus
          rows={1}
          aria-label="Edit message"
          className={cn(
            "block w-full resize-none border-0 bg-transparent px-3 py-2 text-sm",
            "leading-relaxed text-[var(--color-foreground)] focus:outline-none",
          )}
        />
      </div>
      <div className="flex items-center justify-between gap-2">
        <span className="text-[11px] text-[var(--color-muted-foreground)]">
          Enter to save · Shift+Enter for newline · Esc to cancel
        </span>
        <div className="flex items-center gap-1.5">
          <Button size="sm" variant="ghost" onClick={onClose} disabled={mutation.isPending}>
            Cancel
          </Button>
          <Button
            size="sm"
            disabled={!body.trim() || mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? "Saving…" : "Save"}
          </Button>
        </div>
      </div>
    </div>
  );
}
