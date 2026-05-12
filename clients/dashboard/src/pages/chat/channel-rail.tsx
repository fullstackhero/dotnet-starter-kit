import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Hash, Lock, MessageCircle, Plus, Users2 } from "lucide-react";
import {
  createChannel,
  listMyChannels,
  type ChannelDto,
} from "@/api/chat";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/cn";
import { channelTitle } from "@/pages/chat/chat-utils";

/**
 * Editorial sidebar listing the user's channels grouped by type. Active row
 * gets a 2px brand bar (the dashboard's established active-state language).
 * Unread channels render their name in foreground tone with a primary-soft
 * pill counter; read channels recede into muted tone.
 */
export function ChannelRail({
  selectedChannelId,
  onSelect,
  selfUserId,
}: {
  selectedChannelId?: string;
  onSelect: (channelId: string) => void;
  selfUserId?: string;
}) {
  const channelsQuery = useQuery({
    queryKey: ["chat", "my-channels"],
    queryFn: () => listMyChannels({ pageSize: 100 }),
    staleTime: 30_000,
  });

  const channels = channelsQuery.data ?? [];
  const namedChannels = channels.filter((c) => c.type === 2);
  const dms = channels.filter((c) => c.type === 0 || c.type === 1);

  return (
    <aside className="flex h-full w-64 shrink-0 flex-col border-r border-[var(--color-border)] bg-[var(--color-surface-2)]">
      {/* Brand mark + section title */}
      <div className="flex h-14 shrink-0 items-center gap-2 border-b border-[var(--color-border)] px-4">
        <span
          aria-hidden
          className="grid h-7 w-7 place-items-center rounded-md bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
        >
          <MessageCircle className="h-3.5 w-3.5" />
        </span>
        <span className="text-display text-sm font-semibold tracking-tight">Chat</span>
      </div>

      <nav className="min-h-0 flex-1 space-y-3 overflow-y-auto px-2 py-3">
        <Section caption="Channels" actionLabel="New channel">
          {namedChannels.length === 0 && !channelsQuery.isLoading ? (
            <EmptyHint>No channels yet.</EmptyHint>
          ) : (
            namedChannels.map((c) => (
              <ChannelRow
                key={c.id}
                channel={c}
                selfUserId={selfUserId}
                selected={c.id === selectedChannelId}
                onSelect={() => onSelect(c.id)}
              />
            ))
          )}
        </Section>

        <Section caption="Direct Messages">
          {dms.length === 0 && !channelsQuery.isLoading ? (
            <EmptyHint>No conversations.</EmptyHint>
          ) : (
            dms.map((c) => (
              <ChannelRow
                key={c.id}
                channel={c}
                selfUserId={selfUserId}
                selected={c.id === selectedChannelId}
                onSelect={() => onSelect(c.id)}
              />
            ))
          )}
        </Section>
      </nav>

      {/* Footer status row — mirrors the dashboard sidebar's mono caption move. */}
      <div className="border-t border-[var(--color-border)] px-4 py-3">
        <p className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
          {channels.length} channel{channels.length === 1 ? "" : "s"}
        </p>
      </div>
    </aside>
  );
}

function Section({
  caption,
  actionLabel,
  children,
}: {
  caption: string;
  actionLabel?: string;
  children: React.ReactNode;
}) {
  const [createOpen, setCreateOpen] = useState(false);
  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between px-2 pb-1">
        <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          {caption}
        </span>
        {actionLabel && (
          <button
            type="button"
            onClick={() => setCreateOpen(true)}
            title={actionLabel}
            aria-label={actionLabel}
            className={cn(
              "grid h-5 w-5 cursor-pointer place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <Plus className="h-3 w-3" aria-hidden />
          </button>
        )}
      </div>
      <div className="space-y-0.5">{children}</div>
      <CreateChannelDialog open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}

function ChannelRow({
  channel,
  selfUserId,
  selected,
  onSelect,
}: {
  channel: ChannelDto;
  selfUserId?: string;
  selected: boolean;
  onSelect: () => void;
}) {
  const title = channelTitle(channel, selfUserId);
  const hasUnread = channel.unreadCount > 0;
  const Icon =
    channel.type === 2 ? (channel.isPrivate ? Lock : Hash) : Users2;

  return (
    <button
      type="button"
      onClick={onSelect}
      className={cn(
        "group relative flex h-8 w-full cursor-pointer items-center gap-2 rounded-md px-2.5 text-left",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        selected
          ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
          : hasUnread
            ? "text-[var(--color-foreground)] hover:bg-[var(--color-accent)]"
            : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
      )}
    >
      {/* Active 2px brand bar — matches the sidebar's NavItemLink. */}
      <span
        aria-hidden
        className={cn(
          "absolute left-0 top-1/2 h-4 w-0.5 -translate-y-1/2 rounded-r-full bg-[var(--color-primary)]",
          "transition-opacity duration-[var(--duration-default)]",
          selected ? "opacity-100" : "opacity-0",
        )}
      />

      <Icon className="h-3.5 w-3.5 shrink-0" aria-hidden />
      <span
        className={cn(
          "truncate text-sm",
          hasUnread || selected ? "font-medium" : "font-normal",
        )}
      >
        {title}
      </span>
      {hasUnread && !selected && (
        <span
          aria-label={`${channel.unreadCount} unread`}
          className={cn(
            "ml-auto shrink-0 rounded-full px-1.5 py-0.5",
            "font-mono text-[10px] font-semibold tabular-nums",
            "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]",
          )}
        >
          {channel.unreadCount > 99 ? "99+" : channel.unreadCount}
        </span>
      )}
    </button>
  );
}

function EmptyHint({ children }: { children: React.ReactNode }) {
  return (
    <p className="px-2.5 text-xs italic text-[var(--color-muted-foreground)]">
      {children}
    </p>
  );
}

function CreateChannelDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
}) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: () =>
      createChannel({
        name: name.trim(),
        description: description.trim() || null,
        isPrivate,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      setName("");
      setDescription("");
      setIsPrivate(false);
      onOpenChange(false);
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create a channel</DialogTitle>
          <DialogDescription>
            Channels are where teams talk. Anyone in your tenant can join public channels.
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          <div className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="channel-name">Name</Label>
              <Input
                id="channel-name"
                placeholder="engineering, design-feedback…"
                value={name}
                onChange={(e) => setName(e.target.value)}
                maxLength={80}
                autoFocus
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="channel-description">Description (optional)</Label>
              <Input
                id="channel-description"
                placeholder="What's this channel about?"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                maxLength={200}
              />
            </div>
            {/* eslint-disable-next-line jsx-a11y/label-has-associated-control -- input is nested inside the label so implicit association applies; the rule misfires when the visible text sits in nested divs */}
            <label
              htmlFor="channel-private"
              className="flex items-start gap-2.5 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-3 cursor-pointer"
            >
              <input
                id="channel-private"
                type="checkbox"
                className="mt-0.5"
                checked={isPrivate}
                onChange={(e) => setIsPrivate(e.target.checked)}
              />
              <div className="flex-1">
                <div className="text-sm font-medium">Private</div>
                <div className="text-xs text-[var(--color-muted-foreground)]">
                  Only invited members can find or join this channel.
                </div>
              </div>
            </label>
          </div>
        </DialogBody>
        <DialogFooter>
          <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            size="sm"
            disabled={!name.trim() || mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            Create channel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
