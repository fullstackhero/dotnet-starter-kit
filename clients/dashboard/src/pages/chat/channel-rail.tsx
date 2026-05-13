import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Hash, Lock, MessageCircle, Plus, Search, Users2, X } from "lucide-react";
import {
  createChannel,
  findOrCreateDm,
  listMyChannels,
  type ChannelDto,
} from "@/api/chat";
import { searchUsers, type UserDto } from "@/api/identity";
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
import { Avatar } from "@/components/ui/avatar";
import { RealtimeStatusPill } from "@/components/realtime/realtime-status-pill";
import { cn } from "@/lib/cn";
import { useUserDisplay } from "@/lib/use-user-display";
import { channelTitle } from "@/pages/chat/chat-utils";

/**
 * Editorial sidebar listing the user's channels grouped by type. Active row
 * gets a 2px brand bar (the dashboard's established active-state language).
 * Unread channels render their name in foreground tone with a primary-soft
 * pill counter; read channels recede into muted tone.
 *
 * Has a mono-styled filter input at the top — channels matching the query
 * (case-insensitive, by display title) remain visible in both sections.
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
  const [filter, setFilter] = useState("");
  const [createChannelOpen, setCreateChannelOpen] = useState(false);
  const [newDmOpen, setNewDmOpen] = useState(false);

  const channelsQuery = useQuery({
    queryKey: ["chat", "my-channels"],
    queryFn: () => listMyChannels({ pageSize: 100 }),
    staleTime: 30_000,
  });

  const channels = useMemo(() => channelsQuery.data ?? [], [channelsQuery.data]);

  const { namedChannels, dms } = useMemo(() => {
    const f = filter.trim().toLowerCase();
    const match = (c: ChannelDto) =>
      f.length === 0 || channelTitle(c, selfUserId).toLowerCase().includes(f);
    return {
      namedChannels: channels.filter((c) => c.type === 2 && match(c)),
      dms: channels.filter((c) => (c.type === 0 || c.type === 1) && match(c)),
    };
  }, [channels, filter, selfUserId]);

  const filtering = filter.trim().length > 0;
  const totalShown = namedChannels.length + dms.length;

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

      {/* Filter input — editorial mono field, recedes when empty. */}
      <div className="shrink-0 px-3 pb-2 pt-3">
        <label htmlFor="chat-channel-filter" className="relative block">
          <Search
            aria-hidden
            className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
          />
          <input
            id="chat-channel-filter"
            type="text"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            placeholder="Filter channels"
            spellCheck={false}
            autoComplete="off"
            className={cn(
              "h-7 w-full rounded-md border bg-[var(--color-surface-1)] pl-7 pr-7 text-[12.5px]",
              "border-[var(--color-border)] text-[var(--color-foreground)]",
              "placeholder:font-mono placeholder:text-[11px] placeholder:uppercase",
              "placeholder:tracking-[0.10em] placeholder:text-[var(--color-muted-foreground)]",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus:border-[var(--color-primary)] focus:outline-none",
              "focus:ring-2 focus:ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]",
            )}
          />
          {filter.length > 0 && (
            <button
              type="button"
              onClick={() => setFilter("")}
              aria-label="Clear filter"
              className={cn(
                "absolute right-1 top-1/2 grid h-5 w-5 -translate-y-1/2 cursor-pointer place-items-center rounded",
                "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              )}
            >
              <X className="h-3 w-3" aria-hidden />
            </button>
          )}
        </label>
      </div>

      <nav className="min-h-0 flex-1 space-y-3 overflow-y-auto px-2 py-2">
        <Section caption="Channels" onAction={() => setCreateChannelOpen(true)} actionLabel="New channel">
          {channelsQuery.isLoading ? (
            <EmptyHint>Loading…</EmptyHint>
          ) : namedChannels.length === 0 ? (
            <EmptyHint>{filtering ? "No matches." : "No channels yet."}</EmptyHint>
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

        <Section caption="Direct Messages" onAction={() => setNewDmOpen(true)} actionLabel="New direct message">
          {channelsQuery.isLoading ? (
            <EmptyHint>Loading…</EmptyHint>
          ) : dms.length === 0 ? (
            <EmptyHint>{filtering ? "No matches." : "No conversations."}</EmptyHint>
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

      {/* Footer — live connection state + count. */}
      <div className="flex items-center justify-between gap-2 border-t border-[var(--color-border)] px-4 py-2.5">
        <RealtimeStatusPill />
        <span className="font-mono text-[10px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
          {filtering ? (
            <>
              <span className="tabular-nums">{totalShown}</span> of {channels.length}
            </>
          ) : (
            <>
              <span className="tabular-nums">{channels.length}</span> channel{channels.length === 1 ? "" : "s"}
            </>
          )}
        </span>
      </div>

      <CreateChannelDialog open={createChannelOpen} onOpenChange={setCreateChannelOpen} />
      <NewDmDialog
        open={newDmOpen}
        onOpenChange={setNewDmOpen}
        selfUserId={selfUserId}
        onCreated={(id) => onSelect(id)}
      />
    </aside>
  );
}

function Section({
  caption,
  actionLabel,
  onAction,
  children,
}: {
  caption: string;
  actionLabel: string;
  onAction: () => void;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between px-2 pb-1">
        <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          {caption}
        </span>
        <button
          type="button"
          onClick={onAction}
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
      </div>
      <div className="space-y-0.5">{children}</div>
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
  // For 1-on-1 DMs, resolve the partner's real name. Group DMs (type=1) and
  // named channels (type=2) keep channelTitle's fallback formatting.
  const otherDmMember =
    channel.type === 0 ? channel.members.find((m) => m.userId !== selfUserId) : null;
  const dmPartner = useUserDisplay(otherDmMember?.userId);
  const title =
    channel.type === 0 && otherDmMember ? dmPartner.name : channelTitle(channel, selfUserId);
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

      {channel.type === 0 && otherDmMember ? (
        <Avatar
          name={dmPartner.name}
          src={dmPartner.imageUrl ?? null}
          size="xs"
          className="shrink-0"
        />
      ) : (
        <Icon className="h-3.5 w-3.5 shrink-0" aria-hidden />
      )}
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

function NewDmDialog({
  open,
  onOpenChange,
  selfUserId,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  selfUserId?: string;
  onCreated: (channelId: string) => void;
}) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const queryClient = useQueryClient();

  // 250ms debounce — cheap server-side searchUsers call but no need to fire
  // on every keystroke.
  useEffect(() => {
    const t = setTimeout(() => setDebounced(query.trim()), 250);
    return () => clearTimeout(t);
  }, [query]);

  useEffect(() => {
    if (!open) {
      setQuery("");
      setDebounced("");
    }
  }, [open]);

  const usersQuery = useQuery({
    queryKey: ["chat", "user-search", debounced],
    queryFn: () => searchUsers({ search: debounced, pageSize: 8, isActive: true }),
    enabled: open && debounced.length >= 2,
    staleTime: 30_000,
  });

  const createDmMutation = useMutation({
    mutationFn: (userIds: string[]) => findOrCreateDm(userIds),
    onSuccess: (channelId) => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      onOpenChange(false);
      onCreated(channelId);
    },
  });

  const users: UserDto[] = (usersQuery.data?.items ?? []).filter(
    (u) => u.id && u.id !== selfUserId,
  );

  const renderUserName = (u: UserDto): string => {
    const display = [u.firstName, u.lastName].filter(Boolean).join(" ").trim();
    return display || u.userName || u.email || "(unnamed)";
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New direct message</DialogTitle>
          <DialogDescription>
            Find someone in your tenant and we&apos;ll open a DM with them.
            Existing DMs are reused — you won&apos;t create duplicates.
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          <div className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="dm-search">Search</Label>
              <div className="relative">
                <Search
                  aria-hidden
                  className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
                />
                <Input
                  id="dm-search"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Name, username, or email…"
                  className="pl-9"
                  autoFocus
                />
              </div>
            </div>

            <div className="min-h-[160px] rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-1)]">
              {debounced.length < 2 ? (
                <EmptyPickerState label="Type at least 2 characters to search." />
              ) : usersQuery.isLoading ? (
                <EmptyPickerState label="Searching…" mono />
              ) : users.length === 0 ? (
                <EmptyPickerState label={`No one matches "${debounced}".`} />
              ) : (
                <ul className="p-1.5">
                  {users.map((u) => {
                    const display = renderUserName(u);
                    return (
                      <li key={u.id}>
                        <button
                          type="button"
                          disabled={createDmMutation.isPending}
                          onClick={() => createDmMutation.mutate([u.id!])}
                          className={cn(
                            "flex w-full cursor-pointer items-center gap-2.5 rounded-md p-2 text-left",
                            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                            "hover:bg-[var(--color-accent)]",
                            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
                            "disabled:opacity-60",
                          )}
                        >
                          <Avatar name={display} src={u.imageUrl ?? undefined} size="sm" />
                          <div className="min-w-0 flex-1">
                            <div className="truncate text-sm font-medium text-[var(--color-foreground)]">
                              {display}
                            </div>
                            {u.email && (
                              <div className="truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
                                {u.email}
                              </div>
                            )}
                          </div>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          </div>
        </DialogBody>
        <DialogFooter>
          <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function EmptyPickerState({ label, mono }: { label: string; mono?: boolean }) {
  return (
    <div className="flex h-[160px] items-center justify-center px-4 text-center">
      <p
        className={cn(
          "text-xs text-[var(--color-muted-foreground)]",
          mono && "font-mono uppercase tracking-[0.14em]",
        )}
      >
        {label}
      </p>
    </div>
  );
}
