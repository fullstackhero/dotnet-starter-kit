import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  ChevronRight,
  Hash,
  Plus,
  Sparkles,
  Star,
  Users as UsersIcon,
} from "lucide-react";
import { toast } from "sonner";
import {
  createGroup,
  listGroups,
  type GroupDto,
} from "@/api/identity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
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
import { Switch } from "@/components/ui/switch";
import {
  EmptyState,
  ErrorBand,
  Field,
  ListHero,
  Stat,
  StatStrip,
} from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";
import { describe, pad2 } from "@/lib/list-helpers";

export function GroupsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
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

  const groups = query.data ?? [];

  const stats = useMemo(() => {
    if (!query.data) return null;
    const system = groups.filter((g) => g.isSystemGroup).length;
    const defaults = groups.filter((g) => g.isDefault).length;
    const totalMembers = groups.reduce((sum, g) => sum + (g.memberCount ?? 0), 0);
    return {
      total: groups.length,
      system,
      defaults,
      totalMembers,
    };
  }, [query.data, groups]);

  return (
    <div className="space-y-7 pb-12">
      <ListHero
        eyebrow="Identity · Cohorts"
        tenant={user?.tenant ?? "—"}
        subEyebrow="group registry"
        title="Groups"
        totalCount={query.data ? groups.length : null}
        subtitle="Groups bundle members and roles into reusable cohorts. Add a user to a group to grant them every role attached to that group."
        searchValue={search}
        onSearch={setSearch}
        searchPlaceholder="Find a group by name or description…"
        isFetching={query.isFetching}
        onRefresh={() => void query.refetch()}
        ctaLabel="New group"
        onCreate={() => setCreateOpen(true)}
      />

      {stats && stats.total > 0 && (
        <StatStrip cols={3}>
          <Stat label="Total groups" value={pad2(stats.total)} hint={`${stats.system} system · ${stats.total - stats.system} custom`} />
          <Stat
            label="Default groups"
            value={pad2(stats.defaults)}
            hint={stats.defaults === 0 ? "no auto-assign on register" : "auto-assigned to new users"}
            accent
          />
          <Stat
            label="Total memberships"
            value={pad2(stats.totalMembers)}
            hint="across all groups"
          />
        </StatStrip>
      )}

      {query.isError && <ErrorBand message={describe(query.error)} />}

      <section className="fsh-enter fsh-enter-3 space-y-3">
        {query.isLoading ? (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-40 rounded-2xl" style={{ animationDelay: `${i * 50}ms` }} />
            ))}
          </div>
        ) : groups.length === 0 ? (
          <div className={cn("card-shell overflow-hidden rounded-2xl bg-[var(--color-surface-3)]")}>
            <EmptyState
              eyebrow={debounced ? "No matches" : "Empty registry"}
              headline={debounced ? `Nothing matches "${debounced}".` : "No groups defined yet."}
              body={
                debounced
                  ? "Search runs over name and description. Try a different term."
                  : "Create the first group to bundle members + roles. Useful for teams, departments, or feature cohorts."
              }
              icon={<UsersIcon className="h-6 w-6 text-[var(--color-primary)]" />}
              primaryAction={{
                label: debounced ? "Add a new group" : "Add the first group",
                onClick: () => setCreateOpen(true),
                icon: <Sparkles className="h-3.5 w-3.5" />,
              }}
            />
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {groups.map((group, i) => (
              <GroupCard
                key={group.id}
                group={group}
                onOpen={() => navigate(`/identity/groups/${group.id}`)}
                delayMs={Math.min(i, 8) * 30}
              />
            ))}
          </div>
        )}
      </section>

      <CreateGroupDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}

function GroupCard({
  group,
  onOpen,
  delayMs,
}: {
  group: GroupDto;
  onOpen: () => void;
  delayMs: number;
}) {
  return (
    <button
      type="button"
      onClick={onOpen}
      className={cn(
        "fsh-enter group/card relative flex flex-col gap-3 overflow-hidden rounded-2xl text-left",
        "card-shell card-shell-interactive p-5",
        "bg-[var(--color-surface-3)] hover:bg-[var(--color-surface-4)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
        "transition-colors duration-[var(--duration-default)]",
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <span
        aria-hidden
        className="pointer-events-none absolute -right-12 -top-12 h-32 w-32 rounded-full opacity-0 transition-opacity duration-[var(--duration-default)] group-hover/card:opacity-100"
        style={{
          background:
            "radial-gradient(circle, oklch(from var(--color-primary) l c h / 0.18), transparent 70%)",
        }}
      />

      <div className="flex items-start gap-3">
        <span
          aria-hidden
          className={cn(
            "grid h-10 w-10 shrink-0 place-items-center rounded-xl",
            "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.22),oklch(from_var(--color-primary)_l_c_h_/_0.04))]",
            "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.25)]",
            "shadow-[var(--highlight-top)]",
          )}
        >
          <UsersIcon className="h-5 w-5 text-[var(--color-primary)]" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="text-display truncate text-[16px] font-semibold leading-tight tracking-[-0.01em]">
              {group.name}
            </span>
            {group.isDefault && (
              <Badge variant="brand">
                <Star className="h-3 w-3" /> default
              </Badge>
            )}
            {group.isSystemGroup && <Badge variant="outline">system</Badge>}
          </div>
          <p
            className={cn(
              "mt-1 line-clamp-2 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]",
              !group.description && "italic opacity-70",
            )}
          >
            {group.description ?? "No description on file."}
          </p>
        </div>
        <ChevronRight className="h-4 w-4 shrink-0 text-[var(--color-muted-foreground)] transition-colors group-hover/card:text-[var(--color-primary)]" />
      </div>

      <div className="mt-auto flex items-center justify-between border-t border-[var(--color-border)] pt-3">
        <span className="inline-flex items-center gap-1.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          <Hash className="h-3 w-3" /> {group.id.slice(0, 8)}
        </span>
        <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-foreground)]">
          {pad2(group.memberCount)} {group.memberCount === 1 ? "member" : "members"} ·{" "}
          {pad2(group.roleNames?.length ?? 0)} {group.roleNames?.length === 1 ? "role" : "roles"}
        </span>
      </div>
    </button>
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
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              New entry · cohort
            </span>
            <DialogTitle>Create a group</DialogTitle>
            <DialogDescription>
              Groups bundle members and roles. After creating, you'll be taken to the editor to
              attach roles and add members.
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
            <label className="flex items-center justify-between gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-3 py-2.5">
              <span className="min-w-0">
                <span className="block text-sm font-medium tracking-tight">Default group</span>
                <span className="mt-0.5 block text-[12px] text-[var(--color-muted-foreground)]">
                  Newly registered users join automatically.
                </span>
              </span>
              <Switch
                checked={isDefault}
                onCheckedChange={setIsDefault}
                aria-label="Default group"
              />
            </label>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !name.trim()}
              className="brand-glow gradient-sheen gap-1.5"
            >
              <Plus className="h-4 w-4" />
              {mutation.isPending ? "Creating…" : "Create group"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

