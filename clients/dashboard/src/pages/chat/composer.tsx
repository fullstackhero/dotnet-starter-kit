import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ImageIcon, Loader2, Paperclip, Send, X } from "lucide-react";
import { toast } from "sonner";
import { ChannelType, sendMessage, type ChannelTypeValue, type MessageDto } from "@/api/chat";
import { getFileDownloadUrl, Visibility } from "@/api/files";
import { searchUsers, type UserDto } from "@/api/identity";
import { useRealtime } from "@/realtime/realtime-context";
import { formatBytes, useFileUpload } from "@/hooks/use-file-upload";
import { cn } from "@/lib/cn";
import { useUserDisplay } from "@/lib/use-user-display";
import { MentionPicker } from "@/pages/chat/mention-picker";

type PendingAttachment = {
  fileAssetId: string;
  url: string;
  contentType: string;
  fileName: string;
  sizeBytes: number;
};

const ATTACHMENT_MAX_BYTES = 50 * 1024 * 1024; // 50 MB

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
  selfUserId,
  replyTo,
  onClearReply,
}: {
  channelId: string;
  channelTitle: string;
  /** Discriminator for the placeholder: only channels (type=2) get the # prefix. */
  channelType?: ChannelTypeValue;
  /** Caller's user id — used to author the optimistic temp message. */
  selfUserId?: string;
  /** When set, the composer renders a quoted preview of this message and posts
   *  the next send with parentMessageId = replyTo.id. Teams-DM style. */
  replyTo?: MessageDto | null;
  onClearReply?: () => void;
}) {
  const parentMessageId = replyTo?.id;
  const [body, setBody] = useState("");
  const [focused, setFocused] = useState(false);
  const [pendingAttachment, setPendingAttachment] = useState<PendingAttachment | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const queryClient = useQueryClient();
  const realtime = useRealtime();

  // ── File upload ────────────────────────────────────────────────────────
  // ownerType + ownerId match the ChatChannelFileAccessPolicy on the server
  // so channel members get read access to whatever lands in the channel.
  // Category is picked per-file: image vs document; server validates.
  const fileUpload = useFileUpload({
    ownerType: "ChatChannel",
    ownerId: channelId,
    visibility: Visibility.Private,
    category: (file) =>
      file.type.startsWith("image/") ? "Image" : "Document",
    maxBytes: ATTACHMENT_MAX_BYTES,
  });

  const handleFilePick = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = ""; // allow re-picking the same file
    if (!file) return;
    if (pendingAttachment) {
      toast.info("Replace the existing attachment first.");
      return;
    }
    try {
      const asset = await fileUpload.upload(file);
      // Chat attachments are uploaded Private (ChatChannelFileAccessPolicy
      // gates reads), so `asset.publicUrl` is null. Mint a presigned read
      // URL immediately so the resulting message's MessageAttachment.Url
      // satisfies the server's non-empty validation AND the recipient can
      // render the attachment without an extra round-trip. The URL is
      // short-lived; older attachments would need a re-presign on render
      // (future work — make MessageAttachment.Url nullable + resolve at
      // read time via fileAssetId).
      let url = asset.publicUrl ?? "";
      if (!url) {
        const presigned = await getFileDownloadUrl(asset.id, { inline: true });
        url = presigned.url;
      }
      setPendingAttachment({
        fileAssetId: asset.id,
        url,
        contentType: asset.contentType,
        fileName: asset.originalFileName,
        sizeBytes: asset.sizeBytes,
      });
      // Reset progress UI now that the chip carries the state.
      fileUpload.reset();
    } catch {
      // useFileUpload already reflects the error in `progress.status`;
      // surface a toast so the user notices when scrolled away.
      toast.error("Couldn't attach that file.");
    }
  };

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

  // ── Send mutation (optimistic) ─────────────────────────────────────────
  // We insert a temp MessageDto into the channel's cache immediately so the
  // user sees their message land in the feed without waiting for the HTTP
  // round-trip or the SignalR echo. The temp uses an id prefixed with
  // "temp:" so Message can dim it; we replace it with the real DTO once the
  // HTTP response (or the SignalR broadcast) arrives. The two paths
  // converge harmlessly — whichever wins the race patches the cache.
  type SendArgs = {
    text: string;
    clientId: string;
    attachment: PendingAttachment | null;
  };

  const messagesKey = ["chat", "messages", channelId] as const;

  const mutation = useMutation({
    mutationFn: ({ text, attachment }: SendArgs) =>
      sendMessage({
        channelId,
        body: text,
        parentMessageId,
        idempotencyKey: crypto.randomUUID(),
        attachments: attachment ? [attachment] : [],
      }),
    onMutate: ({ text, clientId, attachment }) => {
      const now = new Date().toISOString();
      const temp: MessageDto = {
        id: `temp:${clientId}`,
        channelId,
        authorUserId: selfUserId ?? "",
        body: text,
        parentMessageId: parentMessageId ?? null,
        replyCount: 0,
        editedAtUtc: null,
        deletedAtUtc: null,
        createdAtUtc: now,
        attachments: attachment
          ? [
              {
                id: `temp-att:${clientId}`,
                fileAssetId: attachment.fileAssetId,
                url: attachment.url,
                contentType: attachment.contentType,
                originalFileName: attachment.fileName,
                sizeBytes: attachment.sizeBytes,
              },
            ]
          : [],
        reactions: [],
      };
      queryClient.setQueryData<MessageDto[] | undefined>(messagesKey, (prev) => [
        temp,
        ...(prev ?? []),
      ]);

      setBody("");
      setMention(null);
      setPendingAttachment(null);
      onClearReply?.();
      requestAnimationFrame(() => textareaRef.current?.focus());
    },
    onSuccess: (realMessage, { clientId }) => {
      queryClient.setQueryData<MessageDto[] | undefined>(messagesKey, (prev) => {
        if (!prev) return [realMessage];
        const withoutTemp = prev.filter((m) => m.id !== `temp:${clientId}`);
        if (withoutTemp.some((m) => m.id === realMessage.id)) return withoutTemp;
        return [realMessage, ...withoutTemp];
      });
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
    },
    onError: (_err, { text, clientId, attachment }) => {
      queryClient.setQueryData<MessageDto[] | undefined>(messagesKey, (prev) =>
        prev?.filter((m) => m.id !== `temp:${clientId}`),
      );
      // Restore the text + attachment so the user can retry.
      setBody(text);
      if (attachment) setPendingAttachment(attachment);
      toast.error("Message failed to send — try again.");
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
    if (mutation.isPending) return;
    // Allow send with attachment only — at least one of body / attachment.
    if (!trimmed && !pendingAttachment) return;
    mutation.mutate({
      text: trimmed,
      clientId: crypto.randomUUID(),
      attachment: pendingAttachment,
    });
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
    <div className="relative border-t border-[var(--color-border)] bg-[var(--color-card)] px-4 py-3">
      <div
        className={cn(
          "relative rounded-xl border bg-[var(--color-background)] shadow-xs transition-all",
          "duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          focused
            ? "border-[var(--color-ring)] ring-[3px] ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
            : "border-[var(--color-input)]",
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

        {/* Attachment in flight or staged — sits above the textarea, also
            inside the plinth so it shares the focus halo. */}
        {fileUpload.progress && fileUpload.progress.status !== "done" && (
          <UploadingChip
            fileName={fileUpload.progress.fileName}
            percent={fileUpload.progress.percent}
            status={fileUpload.progress.status}
            error={fileUpload.progress.error}
            onCancel={() => {
              fileUpload.cancel();
              fileUpload.reset();
            }}
          />
        )}
        {pendingAttachment && (
          <PendingAttachmentChip
            attachment={pendingAttachment}
            onClear={() => setPendingAttachment(null)}
          />
        )}

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
              : channelType === ChannelType.Channel
                ? `Message #${channelTitle}`
                : `Message ${channelTitle}`
          }
          aria-label={
            replyTo
              ? "Type your reply"
              : channelType === ChannelType.Channel
                ? `Message channel ${channelTitle}`
                : `Message ${channelTitle}`
          }
          rows={1}
          className={cn(
            "block w-full resize-none border-0 bg-transparent px-4 py-3 pl-12 pr-14 text-sm",
            "leading-relaxed text-[var(--color-foreground)]",
            "placeholder:text-[var(--color-muted-foreground)]",
            "focus:outline-none",
          )}
        />

        {/* Hidden file input — driven by the paperclip below. */}
        <input
          ref={fileInputRef}
          type="file"
          className="sr-only"
          onChange={handleFilePick}
          aria-hidden
          tabIndex={-1}
        />

        {/* Paperclip — bottom-left of the plinth, mirrors the send button on the right. */}
        <button
          type="button"
          aria-label="Attach a file"
          title="Attach a file"
          disabled={fileUpload.isUploading || mutation.isPending}
          onClick={() => fileInputRef.current?.click()}
          className={cn(
            "absolute bottom-2 left-2 grid h-8 w-8 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "disabled:cursor-not-allowed disabled:opacity-50",
          )}
        >
          <Paperclip className="h-3.5 w-3.5" aria-hidden />
        </button>

        <button
          type="button"
          aria-label="Send message"
          disabled={(!body.trim() && !pendingAttachment) || mutation.isPending}
          onClick={send}
          className={cn(
            "absolute bottom-2 right-2 grid h-8 w-8 cursor-pointer place-items-center rounded-md",
            "transition-all duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            (body.trim() || pendingAttachment) && !mutation.isPending
              ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)] hover:scale-105 hover:bg-[var(--color-primary-hover)]"
              : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
          )}
        >
          <Send className="h-3.5 w-3.5" aria-hidden />
        </button>
      </div>

      <div className="mt-1.5 flex items-center justify-between px-1">
        <span className="text-[11px] text-[var(--color-muted-foreground)]">
          {replyTo
            ? "Enter to send · Shift+Enter for newline · Esc to clear"
            : "Enter to send · Shift+Enter for newline · @ + 2 chars to mention"}
        </span>
        {mutation.isError && (
          <span className="text-[11px] font-medium text-[var(--color-destructive)]">
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
        "mx-3 mt-3 flex items-start gap-2.5 rounded-lg border-l-2 px-3 py-2",
        "border-l-[var(--color-primary)] bg-[var(--color-muted)]",
      )}
    >
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5">
          <span className="text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
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
          "grid h-6 w-6 shrink-0 cursor-pointer place-items-center rounded",
          "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        )}
      >
        <X className="h-3 w-3" aria-hidden />
      </button>
    </div>
  );
}

/**
 * Live upload progress chip. Mounts during the preparing / uploading /
 * finalizing phases of useFileUpload; replaced by the ready chip below
 * when the upload lands.
 */
function UploadingChip({
  fileName,
  percent,
  status,
  error,
  onCancel,
}: {
  fileName: string;
  percent: number;
  status: "preparing" | "uploading" | "finalizing" | "done" | "error";
  error?: string;
  onCancel: () => void;
}) {
  const isError = status === "error";
  const label =
    status === "preparing"
      ? "Preparing…"
      : status === "uploading"
        ? `Uploading… ${percent}%`
        : status === "finalizing"
          ? "Finalizing…"
          : isError
            ? error ?? "Upload failed"
            : "Done";
  return (
    <div
      className={cn(
        "mx-3 mt-3 flex items-center gap-2.5 rounded-lg border-l-2 px-3 py-2",
        isError
          ? "border-l-[var(--color-destructive)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)]"
          : "border-l-[var(--color-primary)] bg-[var(--color-muted)]",
      )}
    >
      {isError ? (
        <X className="h-3.5 w-3.5 shrink-0 text-[var(--color-destructive)]" aria-hidden />
      ) : (
        <Loader2
          className="h-3.5 w-3.5 shrink-0 animate-spin text-[var(--color-primary)]"
          aria-hidden
        />
      )}
      <div className="min-w-0 flex-1">
        <p className="truncate text-[12px] font-medium text-[var(--color-foreground)]" title={fileName}>
          {fileName}
        </p>
        <p
          className={cn(
            "text-[11px] tabular-nums",
            isError ? "text-[var(--color-destructive)]" : "text-[var(--color-muted-foreground)]",
          )}
        >
          {label}
        </p>
      </div>
      {/* Inline progress strip, hidden on error. */}
      {!isError && (
        <div
          role="progressbar"
          aria-label="Upload progress"
          aria-valuenow={percent}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuetext={label}
          className="relative h-1 w-16 overflow-hidden rounded-full bg-[var(--color-card)]"
        >
          <span
            aria-hidden
            className="absolute inset-y-0 left-0 bg-[var(--color-primary)] transition-[width] duration-[var(--duration-fast)]"
            style={{ width: `${Math.max(4, percent)}%` }}
          />
        </div>
      )}
      <button
        type="button"
        onClick={onCancel}
        aria-label="Cancel upload"
        title="Cancel"
        className={cn(
          "grid h-6 w-6 shrink-0 cursor-pointer place-items-center rounded focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        )}
      >
        <X className="h-3 w-3" aria-hidden />
      </button>
    </div>
  );
}

/**
 * Ready-to-send attachment chip. Image attachments show a small preview
 * tile; everything else shows a paperclip glyph + name + formatted size.
 * The X removes the attachment without disturbing the typed body.
 */
function PendingAttachmentChip({
  attachment,
  onClear,
}: {
  attachment: PendingAttachment;
  onClear: () => void;
}) {
  const isImage = attachment.contentType.startsWith("image/");
  return (
    <div
      className={cn(
        "mx-3 mt-3 flex items-center gap-2.5 rounded-lg border px-3 py-2",
        "border-[var(--color-border)] bg-[var(--color-muted)]",
      )}
    >
      {isImage && attachment.url ? (
        <img
          src={attachment.url}
          alt=""
          className="h-9 w-9 shrink-0 rounded-lg object-cover ring-1 ring-[var(--color-border)]"
        />
      ) : (
        <span
          aria-hidden
          className={cn(
            "grid h-9 w-9 shrink-0 place-items-center rounded-lg",
            "bg-[var(--color-card)] text-[var(--color-muted-foreground)]",
          )}
        >
          {isImage ? (
            <ImageIcon className="h-4 w-4" />
          ) : (
            <Paperclip className="h-4 w-4" />
          )}
        </span>
      )}
      <div className="min-w-0 flex-1">
        <p
          className="truncate text-[12.5px] font-medium text-[var(--color-foreground)]"
          title={attachment.fileName}
        >
          {attachment.fileName}
        </p>
        <p className="text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {formatBytes(attachment.sizeBytes)} · ready to send
        </p>
      </div>
      <button
        type="button"
        onClick={onClear}
        aria-label="Remove attachment"
        title="Remove attachment"
        className={cn(
          "grid h-6 w-6 shrink-0 cursor-pointer place-items-center rounded focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        )}
      >
        <X className="h-3 w-3" aria-hidden />
      </button>
    </div>
  );
}
