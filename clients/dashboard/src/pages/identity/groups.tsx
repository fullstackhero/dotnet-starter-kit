import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  ChevronRight,
  Plus,
  Search,
  Star,
  UsersRound,
} from "lucide-react";
import { toast } from "sonner";
import {
  createGroup,
  listGroups,
  type GroupDto,
} from "@/api/identity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import { Switch } from "@/components/ui/switch";
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntitySearch,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

const DESKTOP_COLUMNS = "grid-cols-[1fr_160px_120px_24px]";

export function GroupsPage() {
  const [createOpen, setCreateOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const t = setTimeout(() => setDebounced(search.trim()), 250);
    return () => clearTimeout(t);
  }, [search]);

  const query = useQuery({
    queryKey: ["identity", "groups", { search: debounced }],
    queryFn: () => listGroups(debounced || undefined),
  });

  const groups = useMemo(() => query.data ?? [], [query.data]);

  const searchActive = debounced.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={UsersRound}
        title="Groups"
        total={query.data ? groups.length : null}
        unit="group"
        description="Groups bundle members and roles into reusable cohorts. Add a user to a group to grant every role attached to that group."
      >
        <Button
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New group
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Search by name or description…"
      />

      {query.isLoading ? (
        <EntityListLoading rows={6} desktopColumns={DESKTOP_COLUMNS} />
      ) : groups.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : UsersRound}
          title={searchActive ? "No groups found" : "No groups yet"}
          body={
            searchActive
              ? debounced
                ? `Nothing matches "${debounced}". Try a different term.`
                : "No groups match the current filters."
              : "Create the first group to bundle members and roles. Useful for teams, departments, or feature cohorts."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add group
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {groups.length} group{groups.length === 1 ? "" : "s"} found
            </p>
          </div>

          {/* Mobile cards */}
          <div className="space-y-2 md:hidden">
            {groups.map((group) => (
              <MobileGroupCard key={group.id} group={group} />
            ))}
          </div>

          {/* Desktop table */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLUMNS}>
              <span>Group</span>
              <span>Composition</span>
              <span>Flags</span>
              <span />
            </EntityListHeader>
            {groups.map((group, i) => (
              <DesktopGroupRow
                key={group.id}
                group={group}
                isLast={i === groups.length - 1}
              />
            ))}
          </EntityListCard>
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <span>{describe(query.error)}</span>
        </div>
      )}

      <CreateGroupDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}

function MobileGroupCard({ group }: { group: GroupDto }) {
  return (
    <EntityMobileCard
      href={`/identity/groups/${group.id}`}
      aria-label={`Open group ${group.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={group.name} size={40} />
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {group.name}
              </p>
              {group.isDefault && (
                <EntityStatusBadge tone="info">
                  <Star className="mr-0.5 size-2.5" />
                  Default
                </EntityStatusBadge>
              )}
              {group.isSystemGroup && (
                <EntityStatusBadge tone="default">System</EntityStatusBadge>
              )}
            </div>
            <p
              className={cn(
                "mt-0.5 line-clamp-1 text-[12px] text-[var(--color-muted-foreground)]",
                !group.description && "italic opacity-70",
              )}
            >
              {group.description ?? "No description on file."}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-2 text-[11px] text-[var(--color-muted-foreground)]">
        <span>
          {group.memberCount} {group.memberCount === 1 ? "member" : "members"}
        </span>
        <span className="opacity-40">·</span>
        <span>
          {group.roleNames?.length ?? 0}{" "}
          {(group.roleNames?.length ?? 0) === 1 ? "role" : "roles"}
        </span>
      </div>
    </EntityMobileCard>
  );
}

function DesktopGroupRow({
  group,
  isLast,
}: {
  group: GroupDto;
  isLast: boolean;
}) {
  const navigate = useNavigate();
  return (
    <EntityListRow
      className={DESKTOP_COLUMNS}
      isLast={isLast}
      onClick={() => navigate(`/identity/groups/${group.id}`)}
    >
      {/* Name + description */}
      <Link
        to={`/identity/groups/${group.id}`}
        onClick={(e) => e.stopPropagation()}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <EntityInitialsAvatar name={group.name} size={36} />
        <div className="min-w-0">
          <div className="flex items-center gap-1.5">
            <span className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
              {group.name}
            </span>
          </div>
          <p
            className={cn(
              "mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]",
              !group.description && "italic opacity-70",
            )}
          >
            {group.description ?? "No description on file."}
          </p>
        </div>
      </Link>

      {/* Composition */}
      <div className="flex flex-col text-[12px] text-[var(--color-muted-foreground)]">
        <span className="font-mono text-[11px] uppercase tracking-wider text-[var(--color-foreground)]">
          {group.memberCount} {group.memberCount === 1 ? "member" : "members"}
        </span>
        <span className="font-mono text-[11px] uppercase tracking-wider">
          {group.roleNames?.length ?? 0}{" "}
          {(group.roleNames?.length ?? 0) === 1 ? "role" : "roles"}
        </span>
      </div>

      {/* Flags */}
      <div className="flex flex-wrap items-center gap-1.5">
        {group.isDefault && (
          <EntityStatusBadge tone="info">
            <Star className="mr-0.5 size-2.5" />
            Default
          </EntityStatusBadge>
        )}
        {group.isSystemGroup && (
          <EntityStatusBadge tone="default">System</EntityStatusBadge>
        )}
        {!group.isDefault && !group.isSystemGroup && (
          <span className="text-[12px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]">
            —
          </span>
        )}
      </div>

      {/* Trailing chevron */}
      <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
    </EntityListRow>
  );
}

function CreateGroupDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isDefault, setIsDefault] = useState(false);

  useEffect(() => {
    if (!open) {
      setName("");
      setDescription("");
      setIsDefault(false);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      createGroup({
        name: name.trim(),
        description: description.trim() || undefined,
        isDefault,
        roleIds: [],
      }),
    onSuccess: (group) => {
      toast.success("Group created", { description: "Add members and roles next." });
      void queryClient.invalidateQueries({ queryKey: ["identity", "groups"] });
      onClose();
      navigate(`/identity/groups/${group.id}`);
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!name.trim()) return;
    mutation.mutate();
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Create a group</DialogTitle>
            <DialogDescription>
              Groups bundle members and roles. After creating, you'll be taken
              to the editor to attach roles and add members.
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="group-name" label="Name" required>
              <Input
                id="group-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Engineering"
                required
                autoFocus
                maxLength={128}
              />
            </Field>
            <Field
              id="group-description"
              label="Description"
              hint="Helps admins understand what this cohort represents."
            >
              <Input
                id="group-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Engineers across web, mobile, and platform."
                maxLength={512}
              />
            </Field>
            <div className="flex items-center justify-between gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3">
              <div className="min-w-0">
                <span className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  Default group
                </span>
                <span className="mt-0.5 block text-[12.5px] text-[var(--color-muted-foreground)]">
                  Newly registered users join automatically.
                </span>
              </div>
              <Switch
                checked={isDefault}
                onCheckedChange={setIsDefault}
                aria-label="Default group"
              />
            </div>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !name.trim()}>
              {mutation.isPending ? "Creating…" : "Create group"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
