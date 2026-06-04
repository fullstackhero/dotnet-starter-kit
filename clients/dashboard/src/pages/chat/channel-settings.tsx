import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, ShieldCheck, Trash2, UserMinus } from "lucide-react";
import { toast } from "sonner";
import {
  addChannelMembers,
  archiveChannel,
  ChannelMemberRole,
  removeChannelMember,
  updateChannel,
  type ChannelDto,
} from "@/api/chat";
import { searchUsers, type UserDto } from "@/api/identity";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/cn";
import { useUserDisplay } from "@/lib/use-user-display";

/**
 * Channel settings dialog. Three sections, no tabs:
 *   1. General — name + description + privacy (admin-only)
 *   2. Members — add (admin-only) + remove (admin-only on others, self-leave for non-admins)
 *   3. Danger — archive (admin-only) or leave channel (non-admin)
 *
 * Only named channels (type === 2) open this dialog. DMs use their own
 * lightweight settings (not in this iteration).
 */
export function ChannelSettingsDialog({
  open,
  onOpenChange,
  channel,
  selfUserId,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  channel: ChannelDto;
  selfUserId?: string;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const selfMember = channel.members.find((m) => m.userId === selfUserId);
  const isAdmin = selfMember?.role === ChannelMemberRole.Admin;

  // General section — local form state. Initialised from the channel on open;
  // persisted via Save. Discarded if the dialog is dismissed without saving.
  const [name, setName] = useState(channel.name ?? "");
  const [description, setDescription] = useState(channel.description ?? "");
  const [isPrivate, setIsPrivate] = useState(channel.isPrivate);

  useEffect(() => {
    if (!open) return;
    setName(channel.name ?? "");
    setDescription(channel.description ?? "");
    setIsPrivate(channel.isPrivate);
  }, [open, channel.id, channel.name, channel.description, channel.isPrivate]);

  const dirty =
    name.trim() !== (channel.name ?? "").trim() ||
    description.trim() !== (channel.description ?? "").trim() ||
    isPrivate !== channel.isPrivate;

  const saveMutation = useMutation({
    mutationFn: () =>
      updateChannel({
        channelId: channel.id,
        name: name.trim(),
        description: description.trim() || null,
        isPrivate,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "channel", channel.id] });
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      toast.success("Channel updated.");
    },
    onError: () => toast.error("Couldn't save channel changes."),
  });

  const archiveMutation = useMutation({
    mutationFn: () => archiveChannel(channel.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      toast.success("Channel archived.");
      onOpenChange(false);
      navigate("/chat");
    },
    onError: () => toast.error("Couldn't archive the channel."),
  });

  const leaveMutation = useMutation({
    mutationFn: () => removeChannelMember(channel.id, selfUserId!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      toast.success("You left the channel.");
      onOpenChange(false);
      navigate("/chat");
    },
    onError: () => toast.error("Couldn't leave the channel."),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Channel settings</DialogTitle>
          <DialogDescription>
            Manage the channel name, members, and lifecycle.
          </DialogDescription>
        </DialogHeader>
        <DialogBody className="max-h-[60vh] space-y-6 overflow-y-auto">
          {/* ── General ─────────────────────────────────────────────── */}
          <section className="space-y-3">
            <SectionTitle>General</SectionTitle>
            <div className="space-y-1.5">
              <Label htmlFor="channel-settings-name">Name</Label>
              <Input
                id="channel-settings-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={!isAdmin}
                maxLength={80}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="channel-settings-description">Description</Label>
              <Input
                id="channel-settings-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                disabled={!isAdmin}
                placeholder="What's this channel about?"
                maxLength={200}
              />
            </div>
            {/* eslint-disable-next-line jsx-a11y/label-has-associated-control */}
            <label
              htmlFor="channel-settings-private"
              className={cn(
                "flex cursor-pointer items-start gap-2.5 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] p-3",
                !isAdmin && "cursor-not-allowed opacity-60",
              )}
            >
              <input
                id="channel-settings-private"
                type="checkbox"
                className="mt-0.5"
                checked={isPrivate}
                onChange={(e) => setIsPrivate(e.target.checked)}
                disabled={!isAdmin}
              />
              <div className="flex-1">
                <div className="text-sm font-medium">Private</div>
                <div className="text-xs text-[var(--color-muted-foreground)]">
                  Only invited members can find or join this channel.
                </div>
              </div>
            </label>
          </section>

          {/* ── Members ─────────────────────────────────────────────── */}
          <section className="space-y-3">
            <SectionTitle>
              Members
              <span className="ml-2 text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
                {channel.members.length}
              </span>
            </SectionTitle>
            <MemberList
              channel={channel}
              selfUserId={selfUserId}
              isAdmin={isAdmin}
            />
            {isAdmin && <AddMembersRow channel={channel} />}
          </section>

          {/* ── Danger zone ─────────────────────────────────────────── */}
          <section className="space-y-2">
            <SectionTitle>Danger zone</SectionTitle>
            <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] p-3">
              {isAdmin ? (
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <div className="text-sm font-medium">Archive channel</div>
                    <div className="text-xs text-[var(--color-muted-foreground)]">
                      Hides the channel for everyone. Restore from the admin
                      panel if you change your mind.
                    </div>
                  </div>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => {
                      if (
                        window.confirm("Archive this channel for everyone?")
                      ) {
                        archiveMutation.mutate();
                      }
                    }}
                    disabled={archiveMutation.isPending}
                  >
                    <Trash2 className="mr-1 h-3.5 w-3.5" aria-hidden />
                    {archiveMutation.isPending ? "Archiving…" : "Archive"}
                  </Button>
                </div>
              ) : (
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <div className="text-sm font-medium">Leave channel</div>
                    <div className="text-xs text-[var(--color-muted-foreground)]">
                      You&apos;ll stop receiving messages. Rejoin via channel
                      discovery if it&apos;s public.
                    </div>
                  </div>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => {
                      if (window.confirm("Leave this channel?")) {
                        leaveMutation.mutate();
                      }
                    }}
                    disabled={leaveMutation.isPending}
                  >
                    <UserMinus className="mr-1 h-3.5 w-3.5" aria-hidden />
                    {leaveMutation.isPending ? "Leaving…" : "Leave"}
                  </Button>
                </div>
              )}
            </div>
          </section>
        </DialogBody>
        <DialogFooter>
          <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
            Close
          </Button>
          {isAdmin && (
            <Button
              size="sm"
              disabled={!dirty || !name.trim() || saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              {saveMutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function SectionTitle({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex items-center text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
      {children}
    </div>
  );
}

function MemberList({
  channel,
  selfUserId,
  isAdmin,
}: {
  channel: ChannelDto;
  selfUserId?: string;
  isAdmin: boolean;
}) {
  return (
    <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]">
      {channel.members.map((m) => (
        <MemberRow
          key={m.id}
          channelId={channel.id}
          userId={m.userId}
          isSelf={m.userId === selfUserId}
          isAdmin={m.role === ChannelMemberRole.Admin}
          canRemove={isAdmin && m.userId !== selfUserId}
        />
      ))}
    </ul>
  );
}

function MemberRow({
  channelId,
  userId,
  isSelf,
  isAdmin: memberIsAdmin,
  canRemove,
}: {
  channelId: string;
  userId: string;
  isSelf: boolean;
  isAdmin: boolean;
  canRemove: boolean;
}) {
  const display = useUserDisplay(userId);
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => removeChannelMember(channelId, userId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "channel", channelId] });
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      toast.success("Member removed.");
    },
    onError: () => toast.error("Couldn't remove the member."),
  });

  return (
    <li className="flex items-center gap-2.5 px-3 py-2">
      <Avatar
        name={display.name}
        src={display.imageUrl ?? null}
        size="sm"
        className="shrink-0"
      />
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5">
          <span className="truncate text-sm font-medium text-[var(--color-foreground)]">
            {display.name}
            {isSelf && (
              <span className="ml-1.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                you
              </span>
            )}
          </span>
          {memberIsAdmin && (
            <span className="inline-flex items-center gap-0.5 rounded-md bg-[var(--color-primary-soft)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-primary)]">
              <ShieldCheck className="h-2.5 w-2.5" aria-hidden /> admin
            </span>
          )}
        </div>
        {display.handle && (
          <div className="truncate text-[11px] text-[var(--color-muted-foreground)]">
            @{display.handle}
          </div>
        )}
      </div>
      {canRemove && (
        <button
          type="button"
          onClick={() => {
            if (window.confirm(`Remove ${display.name} from the channel?`)) {
              mutation.mutate();
            }
          }}
          disabled={mutation.isPending}
          aria-label={`Remove ${display.name}`}
          className={cn(
            "grid h-8 w-8 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-destructive)] hover:text-[var(--color-destructive-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "disabled:opacity-50",
          )}
        >
          <UserMinus className="h-3.5 w-3.5" aria-hidden />
        </button>
      )}
    </li>
  );
}

function AddMembersRow({ channel }: { channel: ChannelDto }) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const queryClient = useQueryClient();

  useEffect(() => {
    const t = setTimeout(() => setDebounced(query.trim()), 250);
    return () => clearTimeout(t);
  }, [query]);

  const usersQuery = useQuery({
    queryKey: ["chat", "settings-add-search", debounced],
    queryFn: () => searchUsers({ search: debounced, pageSize: 8, isActive: true }),
    enabled: debounced.length >= 2,
    staleTime: 30_000,
  });

  const existingIds = useMemo(
    () => new Set(channel.members.map((m) => m.userId)),
    [channel.members],
  );

  const candidates: UserDto[] = (usersQuery.data?.items ?? []).filter(
    (u) => u.id && !existingIds.has(u.id),
  );

  const addMutation = useMutation({
    mutationFn: (userId: string) => addChannelMembers(channel.id, [userId]),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "channel", channel.id] });
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      setQuery("");
      setDebounced("");
      toast.success("Member added.");
    },
    onError: () => toast.error("Couldn't add the member."),
  });

  return (
    <div className="space-y-2">
      <Label htmlFor="channel-settings-add" className="text-[11px] font-semibold uppercase tracking-wider">
        Add member
      </Label>
      <Input
        id="channel-settings-add"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Name, username, or email…"
      />
      {debounced.length >= 2 && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]">
          {usersQuery.isLoading ? (
            <div className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
              Searching…
            </div>
          ) : candidates.length === 0 ? (
            <div className="px-3 py-3 text-xs italic text-[var(--color-muted-foreground)]">
              No matches outside the current member list.
            </div>
          ) : (
            <ul className="divide-y divide-[var(--color-border)]">
              {candidates.map((u) => {
                const display =
                  [u.firstName, u.lastName].filter(Boolean).join(" ").trim() ||
                  u.userName ||
                  u.email ||
                  "(unnamed)";
                return (
                  <li key={u.id}>
                    <button
                      type="button"
                      disabled={addMutation.isPending}
                      onClick={() => addMutation.mutate(u.id!)}
                      className={cn(
                        "flex w-full cursor-pointer items-center gap-2.5 px-3 py-2 text-left",
                        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                        "hover:bg-[var(--color-accent)] disabled:opacity-60",
                      )}
                    >
                      <Avatar
                        name={display}
                        src={u.imageUrl ?? null}
                        size="sm"
                        className="shrink-0"
                      />
                      <div className="min-w-0 flex-1">
                        <div className="truncate text-sm font-medium text-[var(--color-foreground)]">
                          {display}
                        </div>
                        {u.email && (
                          <div className="truncate text-[11px] text-[var(--color-muted-foreground)]">
                            {u.email}
                          </div>
                        )}
                      </div>
                      <Plus className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" aria-hidden />
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
