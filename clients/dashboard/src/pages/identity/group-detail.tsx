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
  ArrowLeft,
  Hash,
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
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
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
import { ErrorBand, Field } from "@/components/list";
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
        <BackLink />
        <Skeleton className="h-32 rounded-2xl" />
        <Skeleton className="h-64 rounded-2xl" />
      </div>
    );
  }

  if (groupQuery.isError || !group) {
    return (
      <div className="space-y-4">
        <BackLink />
        <ErrorBand message={groupQuery.error ? describe(groupQuery.error) : "Group not found."} />
      </div>
    );
  }

  return (
    <div className="space-y-7 pb-12">
      <BackLink />

      {/* Hero */}
      <section
        className={cn(
          "fsh-enter fsh-enter-1 card-shell relative overflow-hidden rounded-[20px]",
          "bg-[var(--color-surface-3)]",
        )}
      >
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10"
          style={{
            backgroundImage: `
              radial-gradient(60% 70% at 0% 0%, oklch(from var(--color-primary) l c h / 0.15), transparent 60%),
              radial-gradient(50% 60% at 100% 100%, oklch(from var(--color-primary) l c h / 0.07), transparent 70%)
            `,
          }}
        />
        <div className="relative flex flex-col gap-6 px-6 py-7 sm:px-8 sm:py-9 md:flex-row md:items-end md:justify-between md:px-10">
          <div className="flex items-start gap-5">
            <span
              aria-hidden
              className={cn(
                "grid h-14 w-14 shrink-0 place-items-center rounded-2xl",
                "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.22),oklch(from_var(--color-primary)_l_c_h_/_0.04))]",
                "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]",
                "shadow-[var(--highlight-top)]",
              )}
            >
              <UsersIcon className="h-7 w-7 text-[var(--color-primary)]" />
            </span>
            <div className="min-w-0">
              <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                Group · cohort
              </span>
              <h1 className="text-display mt-1 truncate text-[34px] font-semibold leading-[1.05] tracking-[-0.02em] sm:text-[38px]">
                {group.name}
              </h1>
              <div className="mt-2 flex flex-wrap items-center gap-1.5">
                <Badge variant="brand">
                  {pad2(group.memberCount)} {group.memberCount === 1 ? "member" : "members"}
                </Badge>
                <Badge variant="default">
                  {pad2(group.roleNames?.length ?? 0)}{" "}
                  {group.roleNames?.length === 1 ? "role" : "roles"}
                </Badge>
                {group.isDefault && (
                  <Badge variant="brand">
                    <Star className="h-3 w-3" /> default
                  </Badge>
                )}
                {group.isSystemGroup && <Badge variant="outline">system</Badge>}
                <code className="inline-flex items-center gap-1 rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]">
                  <Hash className="h-2.5 w-2.5" /> {group.id}
                </code>
              </div>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2 md:justify-end">
            {!group.isSystemGroup && (
              <Button variant="destructive" size="sm" onClick={() => setConfirmDelete(true)}>
                <Trash2 className="mr-1 h-3.5 w-3.5" /> Delete group
              </Button>
            )}
          </div>
        </div>
      </section>

      <div className="grid gap-5 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        {/* Metadata + roles */}
        <Card className="fsh-enter fsh-enter-2">
          <CardHeader>
            <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Identity
            </span>
            <CardTitle className="text-[15px]">Group details</CardTitle>
            <CardDescription>Name, description, and the roles attached to this group.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4 pt-1">
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
            <div className="flex items-center justify-between gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-3 py-2.5">
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
                <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  Roles attached
                </span>
                <span className="font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
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
          </CardContent>
          <div
            className={cn(
              "flex items-center justify-end gap-2 border-t border-[var(--color-border)] px-6 py-3",
              "bg-[var(--color-surface-2)]",
            )}
          >
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
              className="brand-glow gradient-sheen"
            >
              {save.isPending ? "Saving…" : "Save changes"}
            </Button>
          </div>
        </Card>

        {/* Members */}
        <Card className="fsh-enter fsh-enter-3">
          <CardHeader>
            <div className="flex items-center justify-between gap-3">
              <div>
                <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  Cohort · members
                </span>
                <CardTitle className="mt-1 text-[15px]">Members</CardTitle>
                <CardDescription>
                  Users who belong to this group inherit every role attached above.
                </CardDescription>
              </div>
              <Button size="sm" onClick={() => setAddOpen(true)} className="gap-1.5">
                <UserPlus className="h-3.5 w-3.5" /> Add members
              </Button>
            </div>
          </CardHeader>
          <CardContent className="px-0 pb-0 pt-1">
            {membersQuery.isLoading ? (
              <div className="space-y-2 px-6 pb-5">
                <Skeleton className="h-12 w-full rounded-md" />
                <Skeleton className="h-12 w-full rounded-md" />
                <Skeleton className="h-12 w-full rounded-md" />
              </div>
            ) : membersQuery.isError ? (
              <div className="px-6 pb-5">
                <ErrorBand message={describe(membersQuery.error)} />
              </div>
            ) : members.length === 0 ? (
              <div className="px-6 pb-5 text-sm text-[var(--color-muted-foreground)]">
                No one in this group yet. Click <strong>Add members</strong> to attach users.
              </div>
            ) : (
              <ul className="border-t border-[var(--color-border)]">
                {members.map((member) => (
                  <li
                    key={member.userId}
                    className="flex items-center justify-between gap-3 border-b border-[var(--color-border)] px-6 py-3 last:border-b-0 transition-colors hover:bg-[var(--color-surface-4)]"
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
          </CardContent>
        </Card>
      </div>

      {/* Delete dialog */}
      <Dialog open={confirmDelete} onOpenChange={(o) => (!o ? setConfirmDelete(false) : undefined)}>
        <DialogContent>
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-destructive)]">
              Permanent action
            </span>
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
          : "border-[var(--color-border)] bg-[var(--color-surface-3)] hover:border-[var(--color-border-strong)]",
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
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            Add · members
          </span>
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
                              : "hover:bg-[var(--color-surface-4)]",
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
                          <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
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
            className="brand-glow gradient-sheen gap-1.5"
          >
            <UserPlus className="h-4 w-4" />
            {add.isPending ? "Adding…" : `Add ${picked.size}`}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function BackLink() {
  return (
    <Link
      to="/identity/groups"
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md px-2 py-1 -ml-2 text-[12.5px]",
        "font-medium text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)] hover:bg-[var(--color-accent)]",
        "transition-colors",
      )}
    >
      <ArrowLeft className="h-3.5 w-3.5" /> All groups
    </Link>
  );
}
