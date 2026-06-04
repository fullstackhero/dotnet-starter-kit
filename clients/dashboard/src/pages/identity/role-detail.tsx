import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { toast } from "sonner";
import {
  Check,
  ChevronDown,
  KeyRound,
  Lock,
  Minus,
  Search,
  ShieldCheck,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import {
  deleteRole,
  getPermissionsCatalog,
  getRoleWithPermissions,
  updateRolePermissions,
  upsertRole,
} from "@/api/identity";
import {
  groupPermissions,
  type PermissionDescriptor,
} from "@/api/permissions-catalog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
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

// System roles defined by the framework (RoleConstants.DefaultRoles on the
// server). These cannot be deleted, renamed, re-described, or have their
// permissions edited — the API rejects all four with 400/403, so we mirror
// those rules in the UI as a read-only mode rather than letting the user
// click a destructive action only to be turned away by a toast.
const SYSTEM_ROLE_NAMES: ReadonlyArray<string> = ["Admin", "Basic"];
const isSystemRoleName = (name?: string | null): boolean =>
  !!name && SYSTEM_ROLE_NAMES.includes(name);

export function RoleDetailPage() {
  const { roleId = "" } = useParams<{ roleId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const roleQuery = useQuery({
    queryKey: ["identity", "roles", roleId],
    queryFn: () => getRoleWithPermissions(roleId),
    enabled: !!roleId,
  });

  // Permission catalog — fetched from the server so the editor knows about
  // every module's permissions, not just Identity's. Stable for the session;
  // tenant context only switches on full sign-in/out which kills the cache.
  const catalogQuery = useQuery({
    queryKey: ["identity", "permissions", "catalog"],
    queryFn: getPermissionsCatalog,
    staleTime: 10 * 60 * 1000,
  });
  const catalog = useMemo<PermissionDescriptor[]>(
    () => catalogQuery.data ?? [],
    [catalogQuery.data],
  );
  const catalogGroups = useMemo(() => groupPermissions(catalog), [catalog]);

  const role = roleQuery.data;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [initial, setInitial] = useState<Set<string>>(new Set());
  const [confirmDelete, setConfirmDelete] = useState(false);

  // Permissions editor — browse-friendly state. Search, filter chips, and
  // a Set of explicitly-expanded groups. Search or a non-"all" filter
  // implicitly expands every matching group so results never hide behind
  // a closed accordion.
  type PermFilter = "all" | "enabled" | "modified" | "basic";
  const [searchQuery, setSearchQuery] = useState("");
  const [filter, setFilter] = useState<PermFilter>("all");
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());
  const searchInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!role) return;
    setName(role.name);
    setDescription(role.description ?? "");
    const next = new Set(role.permissions ?? []);
    setSelected(next);
    setInitial(new Set(next));
  }, [role]);

  const dirtyMeta = useMemo(() => {
    if (!role) return false;
    return name.trim() !== role.name || (description ?? "") !== (role.description ?? "");
  }, [role, name, description]);

  const dirtyPerms = useMemo(() => {
    if (selected.size !== initial.size) return true;
    for (const p of selected) if (!initial.has(p)) return true;
    return false;
  }, [selected, initial]);

  const isDirty = dirtyMeta || dirtyPerms;

  const togglePerm = (n: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(n)) next.delete(n);
      else next.add(n);
      return next;
    });
  };

  const setGroupAll = (perms: PermissionDescriptor[], on: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev);
      for (const p of perms) {
        if (on) next.add(p.name);
        else next.delete(p.name);
      }
      return next;
    });
  };

  const presetBasic = () =>
    setSelected(new Set(catalog.filter((p) => p.isBasic).map((p) => p.name)));
  const presetAll = () => setSelected(new Set(catalog.map((p) => p.name)));
  const presetClear = () => setSelected(new Set());

  // ── Editor filter pipeline ──────────────────────────────────────────────
  // Counts for the filter chips — they read against the *current* selection,
  // not the catalog, so "Modified · 3" updates live as the user toggles.
  const modifiedCount = useMemo(() => {
    let n = 0;
    for (const p of catalog) {
      if (selected.has(p.name) !== initial.has(p.name)) n += 1;
    }
    return n;
  }, [selected, initial, catalog]);
  const basicCount = useMemo(
    () => catalog.filter((p) => p.isBasic).length,
    [catalog],
  );

  const matchesSearch = (p: PermissionDescriptor, q: string) => {
    if (!q) return true;
    const needle = q.toLowerCase();
    return (
      p.resource.toLowerCase().includes(needle) ||
      p.action.toLowerCase().includes(needle) ||
      p.description.toLowerCase().includes(needle) ||
      p.name.toLowerCase().includes(needle)
    );
  };
  const matchesFilter = (p: PermissionDescriptor): boolean => {
    switch (filter) {
      case "enabled":
        return selected.has(p.name);
      case "modified":
        return selected.has(p.name) !== initial.has(p.name);
      case "basic":
        return !!p.isBasic;
      default:
        return true;
    }
  };

  // Visible groups: each group keeps only the perms that pass both filters.
  // Groups with zero matches drop out entirely so the accordion stays tight.
  const visibleGroups = useMemo(() => {
    const q = searchQuery.trim();
    return catalogGroups.map((g) => ({
      resource: g.resource,
      permissions: g.permissions.filter((p) => matchesSearch(p, q) && matchesFilter(p)),
    })).filter((g) => g.permissions.length > 0);
    // matchesFilter / matchesSearch use selected/initial/filter/searchQuery;
    // explicit deps so eslint is happy and re-renders only when needed.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchQuery, filter, selected, initial, catalogGroups]);

  // Force-expand when a filter or search is active so results aren't hiding.
  const forceExpand = searchQuery.trim() !== "" || filter !== "all";

  const toggleGroup = (resource: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(resource)) next.delete(resource);
      else next.add(resource);
      return next;
    });
  };
  const expandAllGroups = () =>
    setExpandedGroups(new Set(catalogGroups.map((g) => g.resource)));
  const collapseAllGroups = () => setExpandedGroups(new Set());

  const saveMeta = useMutation({
    mutationFn: () =>
      upsertRole({
        id: roleId,
        name: name.trim(),
        description: description.trim() || undefined,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles"] });
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles", roleId] });
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const savePerms = useMutation({
    mutationFn: () => updateRolePermissions(roleId, Array.from(selected)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles", roleId] });
    },
    onError: (err) => toast.error("Permissions update failed", { description: describe(err) }),
  });

  const removeRole = useMutation({
    mutationFn: () => deleteRole(roleId),
    onSuccess: () => {
      toast.success("Role deleted");
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles"] });
      navigate("/identity/roles");
    },
    onError: (err) => {
      toast.error("Delete failed", { description: describe(err) });
      setConfirmDelete(false);
    },
  });

  const saveAll = async () => {
    try {
      if (dirtyMeta) await saveMeta.mutateAsync();
      if (dirtyPerms) await savePerms.mutateAsync();
      toast.success("Role saved");
    } catch {
      // mutations report their own errors via toast
    }
  };

  const reset = () => {
    if (!role) return;
    setName(role.name);
    setDescription(role.description ?? "");
    setSelected(new Set(role.permissions ?? []));
  };

  const isSaving = saveMeta.isPending || savePerms.isPending;

  // Block on both queries so the permissions editor never renders with an
  // empty catalog (which would look broken: zero groups, 0/0 enabled, etc.).
  if (roleQuery.isLoading || catalogQuery.isLoading) {
    return (
      <div className="space-y-6">
        <EntityDetailBack to="/identity/roles" label="Back to roles" />
        <Skeleton className="h-32 rounded-xl" />
        <Skeleton className="h-96 rounded-xl" />
      </div>
    );
  }

  if (roleQuery.isError || !role) {
    return (
      <div className="space-y-4">
        <EntityDetailBack to="/identity/roles" label="Back to roles" />
        <ErrorBand message={roleQuery.error ? describe(roleQuery.error) : "Role not found."} />
      </div>
    );
  }

  if (catalogQuery.isError) {
    return (
      <div className="space-y-4">
        <EntityDetailBack to="/identity/roles" label="Back to roles" />
        <ErrorBand
          message={`Couldn't load the permission catalog: ${describe(catalogQuery.error)}`}
        />
      </div>
    );
  }

  const totalSelected = selected.size;
  const totalCatalog = catalog.length;
  const isSystem = isSystemRoleName(role.name);

  return (
    <div className="space-y-5 pb-12">
      <EntityDetailBack to="/identity/roles" label="Back to roles" />

      <EntityDetailHero
        avatar={<EntityDetailAvatar name={role.name} icon={ShieldCheck} />}
        title={role.name}
        badges={
          <>
            {isSystem && (
              <Badge variant="outline">
                <Lock className="h-3 w-3" /> System
              </Badge>
            )}
          </>
        }
        subtitle={role.description || (isSystem ? "Built-in role managed by the framework." : "Custom role.")}
        actions={
          <Button
            variant="destructive"
            size="sm"
            onClick={() => setConfirmDelete(true)}
            disabled={isSystem}
            title={isSystem ? "System roles cannot be deleted." : undefined}
          >
            <Trash2 className="mr-1 h-3.5 w-3.5" /> Delete role
          </Button>
        }
        stats={
          <>
            <EntityDetailStat
              icon={KeyRound}
              value={`${pad2(totalSelected)} / ${pad2(totalCatalog)}`}
              label="permissions"
              tone="primary"
            />
          </>
        }
      />

      {isSystem && (
        <div
          role="status"
          aria-live="polite"
          className="flex items-start gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3"
        >
          <span
            aria-hidden
            className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
          >
            <Lock className="h-3.5 w-3.5" />
          </span>
          <div className="min-w-0 text-sm leading-relaxed">
            <p className="font-medium text-[var(--color-foreground)]">
              Built-in role — read only
            </p>
            <p className="mt-0.5 text-[12.5px] text-[var(--color-muted-foreground)]">
              <span className="font-mono font-medium">{role.name}</span> ships with the framework.
              Its name, description, and permissions are managed centrally so the seed contract
              and the runtime permission syncer stay in agreement. Create a custom role if you
              need a different set of grants.
            </p>
          </div>
        </div>
      )}

      {/* Metadata */}
      <EntityDetailSection
        title="Role details"
        icon={ShieldCheck}
        description="The display label and a one-line summary admins will see at assignment time."
      >
        <div className="grid gap-4 md:grid-cols-2">
          <Field id="role-name" label="Name" required>
            <Input
              id="role-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              maxLength={128}
              readOnly={isSystem}
              aria-readonly={isSystem || undefined}
              className={cn(isSystem && "cursor-not-allowed opacity-70")}
            />
          </Field>
          <Field id="role-desc" label="Description">
            <Input
              id="role-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Short description for this role"
              maxLength={512}
              readOnly={isSystem}
              aria-readonly={isSystem || undefined}
              className={cn(isSystem && "cursor-not-allowed opacity-70")}
            />
          </Field>
        </div>
      </EntityDetailSection>

      {/* Permission editor */}
      <EntityDetailSection
        title="Permissions"
        icon={KeyRound}
        description="Search, filter, and toggle individual permissions — or seed a sensible default from a preset. Some root-level permissions may be filtered server-side."
        action={
          <div className="flex flex-wrap gap-1.5">
            <PresetButton
              onClick={presetBasic}
              icon={<Sparkles className="h-3 w-3" />}
              label="Basic"
              disabled={isSystem}
            />
            <PresetButton onClick={presetAll} label="All" disabled={isSystem} />
            <PresetButton onClick={presetClear} label="Clear" disabled={isSystem} />
          </div>
        }
        padded={false}
        footer={
          !isSystem ? (
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
                {isDirty ? (
                  <span className="inline-flex items-center gap-1.5 text-[var(--color-warning)]">
                    <span className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]" />
                    Unsaved changes
                  </span>
                ) : (
                  "All changes saved"
                )}
              </div>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" onClick={reset} disabled={!isDirty || isSaving}>
                  Discard
                </Button>
                <Button
                  size="sm"
                  onClick={saveAll}
                  disabled={!isDirty || isSaving}
                >
                  {isSaving ? "Saving…" : "Save changes"}
                </Button>
              </div>
            </div>
          ) : undefined
        }
      >
        {/* Toolbar — search + filter chips. Sticky-ish at the top of the
            editor card so it stays in reach as the user scrolls long
            group lists. */}
        <div className="border-b border-[var(--color-border)] px-5 py-3">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div className="relative min-w-0 flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
              <input
                ref={searchInputRef}
                type="search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search by resource, action, or description…"
                aria-label="Search permissions"
                className={cn(
                  "h-9 w-full rounded-md border border-[var(--color-input)] bg-transparent pl-9 pr-9",
                  "text-[13px] outline-none transition-colors",
                  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]",
                  "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                )}
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={() => {
                    setSearchQuery("");
                    searchInputRef.current?.focus();
                  }}
                  aria-label="Clear search"
                  className="absolute right-2 top-1/2 grid size-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
                >
                  <X className="size-3" />
                </button>
              )}
            </div>
            <div className="flex items-center gap-1 overflow-x-auto">
              <FilterChip active={filter === "all"} count={totalCatalog} onClick={() => setFilter("all")}>
                All
              </FilterChip>
              <FilterChip
                active={filter === "enabled"}
                count={totalSelected}
                onClick={() => setFilter("enabled")}
                disabled={totalSelected === 0}
              >
                Enabled
              </FilterChip>
              <FilterChip
                active={filter === "modified"}
                count={modifiedCount}
                tone="warning"
                onClick={() => setFilter("modified")}
                disabled={modifiedCount === 0}
              >
                Modified
              </FilterChip>
              <FilterChip
                active={filter === "basic"}
                count={basicCount}
                onClick={() => setFilter("basic")}
              >
                Basic
              </FilterChip>
            </div>
          </div>
        </div>

        {/* Summary strip — running count + expand/collapse-all */}
        <div className="flex items-center justify-between gap-3 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] bg-[var(--color-secondary)] px-5 py-2">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[11.5px]">
            <span className="font-mono font-semibold tabular-nums text-[var(--color-foreground)]">
              {pad2(totalSelected)} / {pad2(totalCatalog)}
            </span>
            <span className="text-[var(--color-muted-foreground)]">enabled</span>
            {modifiedCount > 0 && (
              <span className="inline-flex items-center gap-1 text-[var(--color-warning)]">
                <span aria-hidden className="text-[var(--color-muted-foreground)]">·</span>
                <span aria-hidden className="inline-block size-1.5 rounded-full bg-[var(--color-warning)]" />
                {modifiedCount} modified
              </span>
            )}
            {forceExpand && visibleGroups.length > 0 && (
              <span className="text-[var(--color-muted-foreground)]">
                <span aria-hidden className="mr-1">·</span>
                showing {visibleGroups.reduce((n, g) => n + g.permissions.length, 0)} match
                {visibleGroups.reduce((n, g) => n + g.permissions.length, 0) === 1 ? "" : "es"}
              </span>
            )}
          </div>
          <div className="flex items-center gap-2 text-[11px]">
            <button
              type="button"
              onClick={expandAllGroups}
              disabled={forceExpand}
              className={cn(
                "cursor-pointer font-medium uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]",
                forceExpand && "cursor-not-allowed opacity-40 hover:text-[var(--color-muted-foreground)]",
              )}
            >
              Expand all
            </button>
            <span aria-hidden className="text-[var(--color-border-strong)]">·</span>
            <button
              type="button"
              onClick={collapseAllGroups}
              disabled={forceExpand}
              className={cn(
                "cursor-pointer font-medium uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]",
                forceExpand && "cursor-not-allowed opacity-40 hover:text-[var(--color-muted-foreground)]",
              )}
            >
              Collapse all
            </button>
          </div>
        </div>

        {/* Accordion of resource groups */}
        {visibleGroups.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 px-5 py-16 text-center">
            <span
              aria-hidden
              className="grid size-10 place-items-center rounded-full bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
            >
              <Search className="size-4" />
            </span>
            <div>
              <p className="text-[13px] font-medium text-[var(--color-foreground)]">
                No permissions match
              </p>
              <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
                Try a different term or clear the current filter.
              </p>
            </div>
            {(searchQuery || filter !== "all") && (
              <button
                type="button"
                onClick={() => {
                  setSearchQuery("");
                  setFilter("all");
                  searchInputRef.current?.focus();
                }}
                className={cn(
                  "mt-1 inline-flex h-7 cursor-pointer items-center gap-1 rounded-full px-3 text-[11px] font-medium",
                  "bg-[var(--color-card)] ring-1 ring-inset ring-[var(--color-border)]",
                  "text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                )}
              >
                <X className="size-3" /> Reset filters
              </button>
            )}
          </div>
        ) : (
          <div className="divide-y divide-[var(--color-border)]">
            {visibleGroups.map((group) => {
              const fullGroup = catalogGroups.find((g) => g.resource === group.resource)!;
              const onCount = fullGroup.permissions.filter((p) => selected.has(p.name)).length;
              const total = fullGroup.permissions.length;
              const allOn = onCount === total;
              const someOn = onCount > 0 && !allOn;
              const groupModified = fullGroup.permissions.filter(
                (p) => selected.has(p.name) !== initial.has(p.name),
              ).length;
              const isExpanded = forceExpand || expandedGroups.has(group.resource);
              const visibleCount = group.permissions.length;
              return (
                <PermissionGroupCard
                  key={group.resource}
                  resource={group.resource}
                  visiblePermissions={group.permissions}
                  totalInGroup={total}
                  onCount={onCount}
                  allOn={allOn}
                  someOn={someOn}
                  groupModified={groupModified}
                  isExpanded={isExpanded}
                  showingPartial={visibleCount !== total}
                  visibleCount={visibleCount}
                  onToggleExpand={() => toggleGroup(group.resource)}
                  onSetGroupAll={(on) => setGroupAll(fullGroup.permissions, on)}
                  onTogglePerm={togglePerm}
                  selected={selected}
                  initial={initial}
                  disabled={isSystem}
                />
              );
            })}
          </div>
        )}
      </EntityDetailSection>

      {/* Delete dialog */}
      <Dialog
        open={confirmDelete}
        onOpenChange={(o) => (!o ? setConfirmDelete(false) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete role</DialogTitle>
            <DialogDescription>
              This permanently removes{" "}
              <span className="font-medium text-[var(--color-foreground)]">{role.name}</span>.
              Members currently assigned will lose its permissions immediately.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={removeRole.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => removeRole.mutate()}
              disabled={removeRole.isPending}
            >
              {removeRole.isPending ? "Deleting…" : "Delete role"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function PresetButton({
  onClick,
  label,
  icon,
  disabled,
}: {
  onClick: () => void;
  label: string;
  icon?: React.ReactNode;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={cn(
        "inline-flex h-7 items-center gap-1 rounded-full bg-[var(--color-card)] px-3",
        "ring-1 ring-inset ring-[var(--color-border)]",
        "text-[11px] font-medium text-[var(--color-muted-foreground)]",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        disabled && "cursor-not-allowed opacity-50 hover:bg-[var(--color-card)] hover:text-[var(--color-muted-foreground)]",
      )}
    >
      {icon}
      {label}
    </button>
  );
}

/**
 * FilterChip — pill that toggles a permissions filter. Shows a live count
 * on the right. `tone="warning"` paints the count amber for the Modified
 * chip so unsaved changes are scannable without reading the label.
 */
function FilterChip({
  active,
  count,
  onClick,
  disabled,
  tone,
  children,
}: {
  active: boolean;
  count: number;
  onClick: () => void;
  disabled?: boolean;
  tone?: "warning";
  children: React.ReactNode;
}) {
  const isWarning = tone === "warning" && count > 0;
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-pressed={active}
      className={cn(
        "inline-flex h-7 shrink-0 cursor-pointer items-center gap-1.5 rounded-full px-2.5 text-[11.5px] font-medium",
        "transition-colors duration-[var(--duration-fast)]",
        active
          ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]"
          : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        disabled && "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
      )}
    >
      <span>{children}</span>
      <span
        className={cn(
          "rounded-full px-1.5 py-0.5 text-[10px] font-semibold tabular-nums",
          active
            ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.18)] text-[var(--color-primary)]"
            : isWarning
              ? "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.16)] text-[var(--color-warning)]"
              : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        )}
      >
        {count}
      </span>
    </button>
  );
}

/**
 * PermissionGroupCard — collapsible accordion row for one resource. Header
 * carries a tri-state group checkbox, the resource name, on-count + mini
 * pip bar, "all/none" chip actions, and a chevron. Body lists each perm
 * as a single horizontal row (no 3-col grid — much denser at 200+ scale).
 *
 * When the editor is in search/filter mode, the parent forces `isExpanded`
 * so matches are never hiding behind a closed accordion. In that case the
 * "all/none" actions still toggle the FULL group (not just the visible
 * subset), which is the conventionally safer behaviour — the visible-only
 * count is shown next to the bar so the user can confirm what they're
 * about to do.
 */
function PermissionGroupCard({
  resource,
  visiblePermissions,
  totalInGroup,
  onCount,
  allOn,
  someOn,
  groupModified,
  isExpanded,
  showingPartial,
  visibleCount,
  onToggleExpand,
  onSetGroupAll,
  onTogglePerm,
  selected,
  initial,
  disabled,
}: {
  resource: string;
  visiblePermissions: PermissionDescriptor[];
  totalInGroup: number;
  onCount: number;
  allOn: boolean;
  someOn: boolean;
  groupModified: number;
  isExpanded: boolean;
  showingPartial: boolean;
  visibleCount: number;
  onToggleExpand: () => void;
  onSetGroupAll: (on: boolean) => void;
  onTogglePerm: (name: string) => void;
  selected: Set<string>;
  initial: Set<string>;
  disabled?: boolean;
}) {
  return (
    <div>
      {/* Header — clickable to toggle expand. The group checkbox + chip
          buttons stopPropagation so they don't double-fire. */}
      <div
        role="button"
        tabIndex={0}
        onClick={onToggleExpand}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onToggleExpand();
          }
        }}
        aria-expanded={isExpanded}
        className={cn(
          "group/grouphdr flex w-full cursor-pointer items-center gap-3 px-5 py-3.5 text-left",
          "transition-colors duration-[var(--duration-fast)]",
          "hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.03)]",
          isExpanded && "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.02)]",
        )}
      >
        {/* Group tri-state checkbox */}
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onSetGroupAll(!allOn);
          }}
          disabled={disabled}
          aria-label={`Toggle all ${resource}`}
          className={cn(
            "grid size-5 shrink-0 cursor-pointer place-items-center rounded border transition-colors",
            allOn
              ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
              : someOn
                ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.40)] text-[var(--color-primary-foreground)]"
                : "border-[var(--color-input)] hover:border-[var(--color-foreground)]/40",
            disabled && "cursor-not-allowed opacity-60",
          )}
        >
          {allOn ? <Check className="size-3.5" /> : someOn ? <Minus className="size-3.5" /> : null}
        </button>

        {/* Resource name + meta */}
        <div className="flex min-w-0 flex-1 items-baseline gap-3">
          <span className="font-display text-[14px] font-semibold tracking-tight text-[var(--color-foreground)]">
            {resource}
          </span>
          <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
            {pad2(onCount)} / {pad2(totalInGroup)}
          </span>
          {/* Mini pip bar — one pip per perm in the group, filled if on.
              Visually conveys density at a glance for groups with many
              actions. */}
          <PipBar onCount={onCount} total={totalInGroup} />
          {groupModified > 0 && (
            <span
              className="inline-flex items-center gap-1 text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-warning)]"
              title={`${groupModified} unsaved change${groupModified === 1 ? "" : "s"}`}
            >
              <span aria-hidden className="size-1.5 rounded-full bg-[var(--color-warning)]" />
              {groupModified} changed
            </span>
          )}
          {showingPartial && (
            <span className="text-[10.5px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
              · {visibleCount} match{visibleCount === 1 ? "" : "es"}
            </span>
          )}
        </div>

        {/* All / none chip actions. Each button stops the click from
            bubbling up to the row's expand-toggle handler. */}
        <div className="hidden items-center gap-1 sm:flex">
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onSetGroupAll(true);
            }}
            disabled={disabled || allOn}
            className={cn(
              "cursor-pointer rounded-full px-2 py-0.5 text-[10.5px] font-medium uppercase tracking-wider",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)]",
              (disabled || allOn) && "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
            )}
          >
            All
          </button>
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onSetGroupAll(false);
            }}
            disabled={disabled || onCount === 0}
            className={cn(
              "cursor-pointer rounded-full px-2 py-0.5 text-[10.5px] font-medium uppercase tracking-wider",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)]",
              (disabled || onCount === 0) && "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
            )}
          >
            None
          </button>
        </div>

        {/* Chevron */}
        <ChevronDown
          aria-hidden
          className={cn(
            "size-4 shrink-0 text-[var(--color-muted-foreground)] transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
            isExpanded && "rotate-180",
          )}
        />
      </div>

      {/* Body — list of horizontal permission rows */}
      {isExpanded && (
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {visiblePermissions.map((perm) => {
            const checked = selected.has(perm.name);
            const dirty = checked !== initial.has(perm.name);
            return (
              <PermissionRow
                key={perm.name}
                perm={perm}
                checked={checked}
                dirty={dirty}
                onToggle={() => onTogglePerm(perm.name)}
                disabled={disabled}
              />
            );
          })}
        </div>
      )}
    </div>
  );
}

/**
 * PipBar — n discrete pips, filled left-to-right. Caps at 12 visible pips
 * so wider groups still render at a sensible width; overflow is conveyed
 * by the numeric count to the left of the bar.
 */
function PipBar({ onCount, total }: { onCount: number; total: number }) {
  const cap = Math.min(total, 12);
  const filledVisible = Math.round((onCount / total) * cap);
  return (
    <span aria-hidden className="flex items-center gap-[3px]">
      {Array.from({ length: cap }).map((_, i) => (
        <span
          key={i}
          className={cn(
            "h-2 w-[3px] rounded-[1px] transition-colors",
            i < filledVisible
              ? "bg-[var(--color-primary)]"
              : "bg-[var(--color-border-strong)]",
          )}
        />
      ))}
    </span>
  );
}

/**
 * PermissionRow — one permission as a horizontal scan-line. Click anywhere
 * on the row toggles. The mono action name carries the technical handle,
 * the description carries the human label; basic and modified states get
 * small chips/dots on the right.
 *
 * Indent matches the group header chevron column so the visual hierarchy
 * reads as parent → child without a tree-line.
 */
function PermissionRow({
  perm,
  checked,
  dirty,
  onToggle,
  disabled,
}: {
  perm: PermissionDescriptor;
  checked: boolean;
  dirty: boolean;
  onToggle: () => void;
  disabled?: boolean;
}) {
  return (
    <label
      className={cn(
        "group/row relative flex items-center gap-3 px-5 py-2.5 pl-[3.25rem] text-[12.5px]",
        "transition-colors duration-[var(--duration-fast)]",
        disabled ? "cursor-not-allowed" : "cursor-pointer",
        checked
          ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.04)] hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.07)]"
          : !disabled
            ? "hover:bg-[var(--color-accent)]"
            : "",
      )}
    >
      <input
        type="checkbox"
        className="sr-only"
        checked={checked}
        onChange={onToggle}
        disabled={disabled}
      />
      {/* Custom checkbox */}
      <span
        aria-hidden
        className={cn(
          "grid size-4 shrink-0 place-items-center rounded border transition-all",
          checked
            ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
            : "border-[var(--color-input)] bg-transparent group-hover/row:border-[var(--color-foreground)]/40",
        )}
      >
        {checked && <Check className="size-3" />}
      </span>

      {/* Action handle (mono) */}
      <span
        className={cn(
          "min-w-[110px] shrink-0 truncate font-mono text-[12px] tabular-nums",
          checked ? "text-[var(--color-primary)]" : "text-[var(--color-foreground)]",
        )}
      >
        {perm.action}
      </span>

      {/* Human description */}
      <span
        className={cn(
          "min-w-0 flex-1 truncate",
          checked
            ? "text-[var(--color-foreground)]"
            : "text-[var(--color-muted-foreground)]",
        )}
      >
        {perm.description}
      </span>

      {/* Badges */}
      <span className="flex shrink-0 items-center gap-1.5">
        {perm.isRoot && (
          <span
            className={cn(
              "rounded-full px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-[0.12em]",
              "bg-[oklch(from_var(--color-saffron)_l_c_h_/_0.16)] text-[var(--color-saffron)]",
            )}
            title="Root-level permission. May be filtered server-side."
          >
            root
          </span>
        )}
        {perm.isBasic && (
          <span
            className={cn(
              "rounded-full px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-[0.12em]",
              "bg-[oklch(from_var(--color-info)_l_c_h_/_0.16)] text-[var(--color-info)]",
            )}
          >
            basic
          </span>
        )}
        {dirty && (
          <span
            aria-label="modified"
            className="inline-block size-1.5 rounded-full bg-[var(--color-warning)]"
          />
        )}
      </span>
    </label>
  );
}
