import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  ChevronRight,
  Hash,
  Plus,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { toast } from "sonner";
import { listRoles, upsertRole, type RoleDto } from "@/api/identity";
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

const SYSTEM_ROLE_NAMES = new Set(["admin", "administrator", "basic", "user"]);

function newGuid(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return "00000000-0000-0000-0000-000000000000".replace(/0/g, () =>
    Math.floor(Math.random() * 16).toString(16),
  );
}

export function RolesPage() {
  const { user } = useAuth();
  const [createOpen, setCreateOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const t = setTimeout(() => setDebounced(search.trim().toLowerCase()), 200);
    return () => clearTimeout(t);
  }, [search]);

  const query = useQuery({
    queryKey: ["identity", "roles"],
    queryFn: listRoles,
  });

  const roles = query.data ?? [];

  const filtered = useMemo(() => {
    if (!debounced) return roles;
    return roles.filter(
      (r) =>
        r.name.toLowerCase().includes(debounced) ||
        (r.description ?? "").toLowerCase().includes(debounced),
    );
  }, [roles, debounced]);

  const stats = useMemo(() => {
    if (!query.data) return null;
    const system = roles.filter((r) => SYSTEM_ROLE_NAMES.has(r.name.toLowerCase())).length;
    return {
      total: roles.length,
      system,
      custom: roles.length - system,
    };
  }, [query.data, roles]);

  return (
    <div className="space-y-7 pb-12">
      <ListHero
        eyebrow="Identity · Authority"
        tenant={user?.tenant ?? "—"}
        subEyebrow="role registry"
        title="Roles"
        totalCount={query.data ? roles.length : null}
        subtitle="Roles bundle permissions into named bands of authority. Assign them to members from the user detail page."
        searchValue={search}
        onSearch={setSearch}
        searchPlaceholder="Find a role by name or description…"
        isFetching={query.isFetching}
        onRefresh={() => void query.refetch()}
        ctaLabel="New role"
        onCreate={() => setCreateOpen(true)}
      />

      {stats && stats.total > 0 && (
        <StatStrip cols={3}>
          <Stat label="Total roles" value={pad2(stats.total)} hint="across this tenant" />
          <Stat
            label="System"
            value={pad2(stats.system)}
            hint={stats.system === 0 ? "none seeded" : "shipped with the framework"}
          />
          <Stat
            label="Custom"
            value={pad2(stats.custom)}
            hint={stats.custom === 0 ? "none yet" : "tenant-defined"}
            accent
          />
        </StatStrip>
      )}

      {query.isError && <ErrorBand message={describe(query.error)} />}

      <section className="fsh-enter fsh-enter-3 space-y-3">
        {query.isLoading ? (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-36 rounded-2xl" style={{ animationDelay: `${i * 50}ms` }} />
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <div
            className={cn(
              "card-shell overflow-hidden rounded-2xl",
              "bg-[var(--color-surface-3)]",
            )}
          >
            <EmptyState
              eyebrow={debounced ? "No matches" : "Empty registry"}
              headline={
                debounced
                  ? `Nothing matches "${debounced}".`
                  : "No roles defined yet."
              }
              body={
                debounced
                  ? "Search runs over name and description. Try a different term, or clear the search."
                  : "Create the first role to start grouping permissions. Without roles, members default to no access."
              }
              icon={<ShieldCheck className="h-6 w-6 text-[var(--color-primary)]" />}
              primaryAction={{
                label: debounced ? "Add a new role" : "Add the first role",
                onClick: () => setCreateOpen(true),
                icon: <Sparkles className="h-3.5 w-3.5" />,
              }}
            />
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {filtered.map((role, i) => (
              <RoleCard key={role.id} role={role} delayMs={Math.min(i, 8) * 30} />
            ))}
          </div>
        )}
      </section>

      <CreateRoleDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}

function RoleCard({ role, delayMs }: { role: RoleDto; delayMs: number }) {
  const isSystem = SYSTEM_ROLE_NAMES.has(role.name.toLowerCase());
  return (
    <Link
      to={`/identity/roles/${role.id}`}
      className={cn(
        "fsh-enter group/card relative flex flex-col gap-3 overflow-hidden rounded-2xl",
        "card-shell card-shell-interactive p-5 text-left",
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
            "radial-gradient(circle, oklch(from var(--color-primary) l c h / 0.20), transparent 70%)",
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
          <ShieldCheck className="h-5 w-5 text-[var(--color-primary)]" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="text-display truncate text-[16px] font-semibold leading-tight tracking-[-0.01em]">
              {role.name}
            </span>
            {isSystem && <Badge variant="outline">system</Badge>}
          </div>
          <p
            className={cn(
              "mt-1 line-clamp-2 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]",
              !role.description && "italic opacity-70",
            )}
          >
            {role.description ?? "No description on file."}
          </p>
        </div>
        <ChevronRight className="h-4 w-4 shrink-0 text-[var(--color-muted-foreground)] transition-colors group-hover/card:text-[var(--color-primary)]" />
      </div>

      <div className="mt-auto flex items-center justify-between border-t border-[var(--color-border)] pt-3">
        <span className="inline-flex items-center gap-1.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          <Hash className="h-3 w-3" /> {role.id.slice(0, 8)}
        </span>
        <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] transition-colors group-hover/card:text-[var(--color-primary)]">
          manage →
        </span>
      </div>
    </Link>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create dialog
// ───────────────────────────────────────────────────────────────────────

function CreateRoleDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  useEffect(() => {
    if (!open) {
      setName("");
      setDescription("");
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      upsertRole({
        id: newGuid(),
        name: name.trim(),
        description: description.trim() || undefined,
      }),
    onSuccess: (role) => {
      toast.success("Role created", { description: `Now configure ${role.name}'s permissions.` });
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles"] });
      onClose();
      navigate(`/identity/roles/${role.id}`);
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
              New entry · authority
            </span>
            <DialogTitle>Create a role</DialogTitle>
            <DialogDescription>
              Roles bundle permissions. After creating, you'll be taken to the editor to assign
              permissions and tune access.
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="role-name" label="Name" required>
              <Input
                id="role-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Manager"
                required
                autoFocus
                maxLength={128}
              />
            </Field>
            <Field
              id="role-description"
              label="Description"
              hint="Shown to admins when assigning roles. Plain English helps."
            >
              <Input
                id="role-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Can manage users and assign roles"
                maxLength={512}
              />
            </Field>
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
              {mutation.isPending ? "Creating…" : "Create role"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
