import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { toast } from "sonner";
import {
  Lock,
  Search,
  ShieldCheck,
  Star,
  Trash2,
  UserMinus,
  UserPlus,
  Users as UsersIcon,
  X,
} from "lucide-react";
import {
  addUsersToGroup,
  deleteGroup,
  getGroupById,
  getGroupMembers,
  listRoles,
  removeUserFromGroup,
  searchUsers,
  updateGroup,
  type GroupMemberDto,
  type RoleDto,
  type UserDto,
} from "@/api/identity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Avatar } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  EntityDetailAvatar,
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailSection,
  EntityDetailStat,
  ErrorBand,
  Field,
} from "@/components/list";
import { describe, pad2 } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

function memberDisplay(m: GroupMemberDto): string {
  const parts = [m.firstName, m.lastName].filter(Boolean);
  if (parts.length > 0) return parts.join(" ");
  return m.userName ?? m.email ?? "Unknown user";
}

function userDisplay(u: UserDto): string {
  const parts = [u.firstName, u.lastName].filter(Boolean);
  if (parts.length > 0) return parts.join(" ");
  return u.userName ?? u.email ?? "Unknown user";
}

export function GroupDetailPage() {
  const { groupId = "" } = useParams<{ groupId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const groupQuery = useQuery({
    queryKey: ["identity", "groups", groupId],
    queryFn: () => getGroupById(groupId),
    enabled: !!groupId,
  });

  const membersQuery = useQuery({
    queryKey: ["identity", "groups", groupId, "members"],
    queryFn: () => getGroupMembers(groupId),
    enabled: !!groupId,
  });

  const rolesQuery = useQuery({
    queryKey: ["identity", "roles"],
    queryFn: listRoles,
    staleTime: 60_000,
  });

  const group = groupQuery.data;
  const members = membersQuery.data ?? [];
  const roles = rolesQuery.data ?? [];

  // Metadata + role edit state
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isDefault, setIsDefault] = useState(false);
  const [selectedRoleIds, setSelectedRoleIds] = useState<Set<string>>(new Set());
  const [initialRoleIds, setInitialRoleIds] = useState<Set<string>>(new Set());
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [addOpen, setAddOpen] = useState(false);

  useEffect(() => {
    if (!group) return;
    setName(group.name);
    setDescription(group.description ?? "");
    setIsDefault(group.isDefault);
    const next = new Set(group.roleIds ?? []);
    setSelectedRoleIds(next);
    setInitialRoleIds(new Set(next));
  }, [group]);

  const dirtyMeta = useMemo(() => {
    if (!group) return false;
    return (
      name.trim() !== group.name ||
      (description ?? "") !== (group.description ?? "") ||
      isDefault !== group.isDefault
    );
  }, [group, name, description, isDefault]);

  const dirtyRoles = useMemo(() => {
    if (selectedRoleIds.size !== initialRoleIds.size) return true;
    for (const id of selectedRoleIds) if (!initialRoleIds.has(id)) return true;
    return false;
  }, [selectedRoleIds, initialRoleIds]);

  const isDirty = dirtyMeta || dirtyRoles;

  const toggleRole = (roleId: string) => {
    setSelectedRoleIds((prev) => {
      const next = new Set(prev);
      if (next.has(roleId)) next.delete(roleId);
      else next.add(roleId);
      return next;
    });
  };

  const save = useMutation({
    mutationFn: () =>
      updateGroup(groupId, {
        name: name.trim(),
        description: description.trim() || undefined,
        isDefault,
        roleIds: Array.from(selectedRoleIds),
      }),
    onSuccess: () => {
      toast.success("Group updated");
      void queryClient.invalidateQueries({ queryKey: ["identity", "groups"] });
      void queryClient.invalidateQueries({ queryKey: ["identity", "groups", groupId] });
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const remove = useMutation({
    mutationFn: () => deleteGroup(groupId),
    onSuccess: () => {
      toast.success("Group deleted");
      void queryClient.invalidateQueries({ queryKey: ["identity", "groups"] });
      navigate("/identity/groups");
    },
    onError: (err) => {
      toast.error("Delete failed", { description: describe(err) });
      setConfirmDelete(false);
    },
  });

  const removeMember = useMutation({
    mutationFn: (userId: string) => removeUserFromGroup(groupId, userId),
    onSuccess: () => {
      toast.success("Member removed");
      void queryClient.invalidateQueries({
        queryKey: ["identity", "groups", groupId, "members"],
      });
      void queryClient.invalidateQueries({ queryKey: ["identity", "groups", groupId] });
    },
    onError: (err) => toast.error("Remove failed", { description: describe(err) }),
  });

  const reset = () => {
    if (!group) return;
    setName(group.name);
    setDescription(group.description ?? "");
    setIsDefault(group.isDefault);
    setSelectedRoleIds(new Set(group.roleIds ?? []));
  };

  if (groupQuery.isLoading) {
    return (
      <div className="space-y-6">
        <EntityDetailBack to="/identity/groups" label="Back to groups" />
        <Skeleton className="h-32 rounded-xl" />
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  }

  if (groupQuery.isError || !group) {
    return (
      <div className="space-y-4">
        <EntityDetailBack to="/identity/groups" label="Back to groups" />
        <ErrorBand message={groupQuery.error ? describe(groupQuery.error) : "Group not found."} />
      </div>
    );
  }

  return (
    <div className="space-y-5 pb-12">
      <EntityDetailBack to="/identity/groups" label="Back to groups" />

      <EntityDetailHero
        avatar={<EntityDetailAvatar name={group.name} icon={UsersIcon} />}
        title={group.name}
        badges={
          <>
            {group.isDefault && (
              <Badge variant="brand">
                <Star className="h-3 w-3" /> Default
              </Badge>
            )}
            {group.isSystemGroup && (
              <Badge variant="outline">
                <Lock className="h-3 w-3" /> System
              </Badge>
            )}
          </>
        }
        subtitle={group.description || "Group cohort."}
        actions={
          !group.isSystemGroup ? (
            <Button variant="destructive" size="sm" onClick={() => setConfirmDelete(true)}>
              <Trash2 className="mr-1 h-3.5 w-3.5" /> Delete group
            </Button>
          ) : undefined
        }
        stats={
          <>
            <EntityDetailStat
              icon={UsersIcon}
              value={group.memberCount}
              label={group.memberCount === 1 ? "member" : "members"}
              tone="primary"
            />
            <EntityDetailStat
              icon={ShieldCheck}
              value={group.roleNames?.length ?? 0}
              label={group.roleNames?.length === 1 ? "role" : "roles"}
            />
          </>
        }
      />

      <div className="grid gap-5 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        {/* Metadata + roles */}
        <EntityDetailSection
          title="Group details"
          icon={UsersIcon}
          description="Name, description, and the roles attached to this group."
          footer={
            <div className="flex items-center justify-end gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={reset}
                disabled={!isDirty || save.isPending}
              >
                Discard
              </Button>
              <Button
                size="sm"
                onClick={() => save.mutate()}
                disabled={!isDirty || save.isPending}
              >
                {save.isPending ? "Saving…" : "Save changes"}
              </Button>
            </div>
          }
        >
          <div className="space-y-4">
            <Field id="g-name" label="Name" required>
              <Input
                id="g-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={group.isSystemGroup}
                maxLength={128}
              />
            </Field>
            <Field id="g-desc" label="Description">
              <Input
                id="g-desc"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Short summary of who or what this group represents"
                maxLength={512}
              />
            </Field>
            <div className="flex items-center justify-between gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2.5">
              <div className="min-w-0">
                <span className="block text-sm font-medium tracking-tight">Default group</span>
                <span className="mt-0.5 block text-[12px] text-[var(--color-muted-foreground)]">
                  Auto-assign to newly registered users.
                </span>
              </div>
              <Switch
                checked={isDefault}
                onCheckedChange={setIsDefault}
                aria-label="Default group"
              />
            </div>

            {/* Roles attached */}
            <div className="pt-2">
              <div className="mb-2 flex items-center justify-between">
                <span className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
                  Roles attached
                </span>
                <span className="text-[11px] text-[var(--color-muted-foreground)]">
                  {pad2(selectedRoleIds.size)} / {pad2(roles.length)}
                </span>
              </div>
              {rolesQuery.isLoading ? (
                <Skeleton className="h-20 w-full rounded-md" />
              ) : roles.length === 0 ? (
                <p className="text-sm text-[var(--color-muted-foreground)]">
                  No roles defined.{" "}
                  <Link
                    to="/identity/roles"
                    className="underline hover:text-[var(--color-foreground)]"
                  >
                    Create one
                  </Link>{" "}
                  first.
                </p>
              ) : (
                <ul className="grid gap-1.5">
                  {roles.map((role) => (
                    <RoleToggleRow
                      key={role.id}
                      role={role}
                      selected={selectedRoleIds.has(role.id)}
                      onToggle={() => toggleRole(role.id)}
                    />
                  ))}
                </ul>
              )}
            </div>
          </div>
        </EntityDetailSection>

        {/* Members */}
        <EntityDetailSection
          title="Members"
          icon={UsersIcon}
          description="Users who belong to this group inherit every role attached above."
          action={
            <Button size="sm" onClick={() => setAddOpen(true)} className="gap-1.5">
              <UserPlus className="h-3.5 w-3.5" /> Add members
            </Button>
          }
          padded={false}
        >
          {membersQuery.isLoading ? (
            <div className="space-y-2 p-5">
              <Skeleton className="h-12 w-full rounded-md" />
              <Skeleton className="h-12 w-full rounded-md" />
              <Skeleton className="h-12 w-full rounded-md" />
            </div>
          ) : membersQuery.isError ? (
            <div className="p-5">
              <ErrorBand message={describe(membersQuery.error)} />
            </div>
          ) : members.length === 0 ? (
            <div className="p-5 text-sm text-[var(--color-muted-foreground)]">
              No one in this group yet. Click <strong>Add members</strong> to attach users.
            </div>
          ) : (
            <ul>
              {members.map((member) => (
                <li
                  key={member.userId}
                  className="flex items-center justify-between gap-3 border-b border-[var(--color-border)] px-5 py-3 last:border-b-0 transition-colors hover:bg-[var(--color-accent)]"
                >
                  <Link
                    to={`/identity/users/${member.userId}`}
                    className="flex min-w-0 flex-1 items-center gap-3"
                  >
                    <Avatar name={memberDisplay(member)} size="sm" />
                    <div className="min-w-0">
                      <div className="truncate text-sm font-medium tracking-tight">
                        {memberDisplay(member)}
                      </div>
                      {member.email && (
                        <div className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {member.email}
                        </div>
                      )}
                    </div>
                  </Link>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => removeMember.mutate(member.userId)}
                    disabled={removeMember.isPending}
                    className="shrink-0 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"
                  >
                    <UserMinus className="mr-1 h-3.5 w-3.5" /> Remove
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </EntityDetailSection>
      </div>

      {/* Delete dialog */}
      <Dialog open={confirmDelete} onOpenChange={(o) => (!o ? setConfirmDelete(false) : undefined)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete group</DialogTitle>
            <DialogDescription>
              <span className="font-medium text-[var(--color-foreground)]">{group.name}</span>{" "}
              will be removed. Members will lose any permissions inherited through this group.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={remove.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => remove.mutate()}
              disabled={remove.isPending}
            >
              {remove.isPending ? "Deleting…" : "Delete group"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AddMembersDialog
        open={addOpen}
        groupId={groupId}
        existingMemberIds={new Set(members.map((m) => m.userId))}
        onClose={() => setAddOpen(false)}
      />
    </div>
  );
}

function RoleToggleRow({
  role,
  selected,
  onToggle,
}: {
  role: RoleDto;
  selected: boolean;
  onToggle: () => void;
}) {
  return (
    <li
      className={cn(
        "flex items-center justify-between gap-3 rounded-md border px-3 py-2",
        "transition-colors duration-[var(--duration-fast)]",
        selected
          ? "border-[oklch(from_var(--color-primary)_l_c_h_/_0.30)] bg-[var(--color-primary-soft)]"
          : "border-[var(--color-border)] bg-[var(--color-card)] hover:border-[var(--color-border-strong)]",
      )}
    >
      <div className="flex min-w-0 items-center gap-2.5">
        <ShieldCheck
          className={cn(
            "h-4 w-4 shrink-0",
            selected ? "text-[var(--color-primary)]" : "text-[var(--color-muted-foreground)]",
          )}
        />
        <div className="min-w-0">
          <div className="truncate text-sm font-medium tracking-tight">{role.name}</div>
          {role.description && (
            <div className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
              {role.description}
            </div>
          )}
        </div>
      </div>
      <Switch
        checked={selected}
        onCheckedChange={onToggle}
        aria-label={`Attach ${role.name}`}
      />
    </li>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Add members dialog
// ───────────────────────────────────────────────────────────────────────

function AddMembersDialog({
  open,
  groupId,
  existingMemberIds,
  onClose,
}: {
  open: boolean;
  groupId: string;
  existingMemberIds: Set<string>;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [picked, setPicked] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (!open) {
      setSearch("");
      setDebounced("");
      setPicked(new Set());
    }
  }, [open]);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(search.trim()), 250);
    return () => clearTimeout(t);
  }, [search]);

  const usersQuery = useQuery({
    queryKey: ["identity", "users", "search", { search: debounced }],
    queryFn: () =>
      searchUsers({
        pageNumber: 1,
        pageSize: 20,
        search: debounced || undefined,
        isActive: true,
      }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const candidates = (usersQuery.data?.items ?? []).filter(
    (u): u is UserDto & { id: string } => !!u.id,
  );

  const togglePick = (userId: string) => {
    setPicked((prev) => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  };

  const add = useMutation({
    mutationFn: () => addUsersToGroup(groupId, Array.from(picked)),
    onSuccess: (data) => {
      const dupes = data.alreadyMemberUserIds.length;
      const added = data.addedCount;
      toast.success(
        `Added ${added} member${added === 1 ? "" : "s"}` +
          (dupes > 0 ? ` · ${dupes} already present` : ""),
      );
      void queryClient.invalidateQueries({
        queryKey: ["identity", "groups", groupId, "members"],
      });
      void queryClient.invalidateQueries({ queryKey: ["identity", "groups", groupId] });
      onClose();
    },
    onError: (err) => toast.error("Add failed", { description: describe(err) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <DialogHeader>
          <DialogTitle>Pick members to add</DialogTitle>
          <DialogDescription>
            Search active users and select one or more to attach to this group.
          </DialogDescription>
        </DialogHeader>
        <DialogBody className="space-y-3">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-[var(--color-muted-foreground)]" />
            <Input
              autoFocus
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by name, username, or email…"
              className="pl-9"
            />
            {search && (
              <button
                type="button"
                onClick={() => setSearch("")}
                aria-label="Clear search"
                className="absolute right-2 top-2 grid h-5 w-5 place-items-center rounded text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]"
              >
                <X className="h-3 w-3" />
              </button>
            )}
          </div>
          <div className="max-h-[320px] overflow-y-auto rounded-md border border-[var(--color-border)]">
            {usersQuery.isLoading ? (
              <div className="space-y-2 p-3">
                <Skeleton className="h-10 w-full rounded-md" />
                <Skeleton className="h-10 w-full rounded-md" />
                <Skeleton className="h-10 w-full rounded-md" />
              </div>
            ) : candidates.length === 0 ? (
              <div className="p-6 text-center text-sm text-[var(--color-muted-foreground)]">
                {debounced ? `No users match "${debounced}".` : "No users available."}
              </div>
            ) : (
              <ul>
                {candidates.map((user) => {
                  const already = existingMemberIds.has(user.id);
                  const isPicked = picked.has(user.id);
                  return (
                    <li key={user.id}>
                      <button
                        type="button"
                        onClick={() => !already && togglePick(user.id)}
                        disabled={already}
                        className={cn(
                          "flex w-full items-center gap-3 border-b border-[var(--color-border)] px-3 py-2 text-left last:border-b-0",
                          "transition-colors",
                          already
                            ? "opacity-50"
                            : isPicked
                              ? "bg-[var(--color-primary-soft)]"
                              : "hover:bg-[var(--color-accent)]",
                        )}
                      >
                        <span
                          className={cn(
                            "grid h-4 w-4 shrink-0 place-items-center rounded border",
                            isPicked
                              ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                              : "border-[var(--color-input)]",
                          )}
                        >
                          {isPicked && <span className="text-[10px] leading-none">✓</span>}
                        </span>
                        <Avatar name={userDisplay(user)} size="sm" />
                        <div className="min-w-0 flex-1">
                          <div className="truncate text-sm font-medium">{userDisplay(user)}</div>
                          <div className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
                            {user.email ?? user.userName}
                          </div>
                        </div>
                        {already && (
                          <span className="text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                            already in
                          </span>
                        )}
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
          <div className="text-[12px] text-[var(--color-muted-foreground)]">
            {picked.size} selected
          </div>
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={add.isPending}>
              Cancel
            </Button>
          </DialogClose>
          <Button
            onClick={() => add.mutate()}
            disabled={picked.size === 0 || add.isPending}
            className="gap-1.5"
          >
            <UserPlus className="h-4 w-4" />
            {add.isPending ? "Adding…" : `Add ${picked.size}`}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
