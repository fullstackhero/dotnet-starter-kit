import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { toast } from "sonner";
import {
  ArrowLeft,
  Check,
  Hash,
  Lock,
  Minus,
  ShieldCheck,
  Sparkles,
  Trash2,
} from "lucide-react";
import {
  deleteRole,
  getRoleWithPermissions,
  updateRolePermissions,
  upsertRole,
} from "@/api/identity";
import {
  IDENTITY_PERMISSIONS,
  PERMISSION_GROUPS,
  type PermissionDescriptor,
} from "@/api/permissions-catalog";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
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
import { ErrorBand, Field } from "@/components/list";
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

  const role = roleQuery.data;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [initial, setInitial] = useState<Set<string>>(new Set());
  const [confirmDelete, setConfirmDelete] = useState(false);

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
    setSelected(new Set(IDENTITY_PERMISSIONS.filter((p) => p.isBasic).map((p) => p.name)));
  const presetAll = () => setSelected(new Set(IDENTITY_PERMISSIONS.map((p) => p.name)));
  const presetClear = () => setSelected(new Set());

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

  if (roleQuery.isLoading) {
    return (
      <div className="space-y-6">
        <BackLink />
        <Skeleton className="h-32 rounded-2xl" />
        <Skeleton className="h-96 rounded-2xl" />
      </div>
    );
  }

  if (roleQuery.isError || !role) {
    return (
      <div className="space-y-4">
        <BackLink />
        <ErrorBand message={roleQuery.error ? describe(roleQuery.error) : "Role not found."} />
      </div>
    );
  }

  const totalSelected = selected.size;
  const totalCatalog = IDENTITY_PERMISSIONS.length;
  const isSystem = isSystemRoleName(role.name);

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
              <ShieldCheck className="h-7 w-7 text-[var(--color-primary)]" />
            </span>
            <div className="min-w-0">
              <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                {isSystem ? "System role · permissions" : "Role · permissions"}
              </span>
              <h1 className="text-display mt-1 flex items-center gap-3 truncate text-[34px] font-semibold leading-[1.05] tracking-[-0.02em] sm:text-[38px]">
                <span className="truncate">{role.name}</span>
                {isSystem && (
                  <span
                    aria-hidden
                    title="System role — managed by the framework"
                    className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
                  >
                    <Lock className="h-3.5 w-3.5" />
                  </span>
                )}
              </h1>
              <div className="mt-2 flex flex-wrap items-center gap-1.5">
                <Badge variant="brand">
                  {pad2(totalSelected)} / {pad2(totalCatalog)} permissions
                </Badge>
                {isSystem && <Badge variant="outline">system</Badge>}
                <code className="inline-flex items-center gap-1 rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]">
                  <Hash className="h-2.5 w-2.5" /> {role.id}
                </code>
              </div>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2 md:justify-end">
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setConfirmDelete(true)}
              disabled={isSystem}
              title={isSystem ? "System roles cannot be deleted." : undefined}
            >
              <Trash2 className="mr-1 h-3.5 w-3.5" /> Delete role
            </Button>
          </div>
        </div>
      </section>

      {isSystem && (
        <section
          role="status"
          aria-live="polite"
          className="fsh-enter fsh-enter-1 flex items-start gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-2)] px-4 py-3"
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
        </section>
      )}

      {/* Metadata */}
      <Card className="fsh-enter fsh-enter-2">
        <CardHeader>
          <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            Identity
          </span>
          <CardTitle className="text-[15px]">Role details</CardTitle>
          <CardDescription>The display label and a one-line summary admins will see at assignment time.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4 pt-1 md:grid-cols-2">
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
        </CardContent>
      </Card>

      {/* Permission editor */}
      <Card className="fsh-enter fsh-enter-3 overflow-hidden">
        <CardHeader>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                Authority · grants
              </span>
              <CardTitle className="mt-1 text-[15px]">Permissions</CardTitle>
              <CardDescription>
                Toggle individual permissions, or use a preset to seed a sensible default. Some
                root-level permissions may be filtered server-side.
              </CardDescription>
            </div>
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
          </div>
        </CardHeader>
        <CardContent className="px-0 pb-0 pt-0">
          <div className="border-t border-[var(--color-border)] divide-y divide-[var(--color-border)]">
            {PERMISSION_GROUPS.map((group) => {
              const onCount = group.permissions.filter((p) => selected.has(p.name)).length;
              const allOn = onCount === group.permissions.length;
              const someOn = onCount > 0 && !allOn;
              return (
                <div key={group.resource} className="px-6 py-5 sm:px-8">
                  {/* Group header */}
                  <div className="mb-3 flex items-center justify-between gap-3">
                    <div className="flex min-w-0 items-center gap-3">
                      <button
                        type="button"
                        onClick={() => setGroupAll(group.permissions, !allOn)}
                        disabled={isSystem}
                        className={cn(
                          "grid h-5 w-5 shrink-0 place-items-center rounded border transition-colors",
                          allOn
                            ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                            : someOn
                              ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.40)] text-[var(--color-primary-foreground)]"
                              : "border-[var(--color-input)] hover:border-[var(--color-foreground)]/40",
                          isSystem && "cursor-not-allowed opacity-60",
                        )}
                        aria-label={`Toggle all ${group.resource}`}
                      >
                        {allOn ? (
                          <Check className="h-3.5 w-3.5" />
                        ) : someOn ? (
                          <Minus className="h-3.5 w-3.5" />
                        ) : null}
                      </button>
                      <h4 className="text-display text-[14px] font-semibold tracking-[-0.005em]">
                        {group.resource}
                      </h4>
                      <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                        {pad2(onCount)} / {pad2(group.permissions.length)}
                      </span>
                    </div>
                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => setGroupAll(group.permissions, true)}
                        disabled={isSystem}
                        className={cn(
                          "rounded-full px-2 py-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
                          isSystem && "cursor-not-allowed opacity-50 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
                        )}
                      >
                        all
                      </button>
                      <button
                        type="button"
                        onClick={() => setGroupAll(group.permissions, false)}
                        disabled={isSystem}
                        className={cn(
                          "rounded-full px-2 py-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
                          isSystem && "cursor-not-allowed opacity-50 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
                        )}
                      >
                        none
                      </button>
                    </div>
                  </div>

                  {/* Permissions grid */}
                  <div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
                    {group.permissions.map((perm) => {
                      const checked = selected.has(perm.name);
                      const wasInitial = initial.has(perm.name);
                      const dirty = checked !== wasInitial;
                      return (
                        <PermissionTile
                          key={perm.name}
                          perm={perm}
                          checked={checked}
                          dirty={dirty}
                          onToggle={() => togglePerm(perm.name)}
                          disabled={isSystem}
                        />
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        </CardContent>

        {/* Sticky save bar — hidden entirely for system roles since
            the dirty/save flow does not apply when nothing can be edited. */}
        {!isSystem && (
          <div
            className={cn(
              "sticky bottom-0 flex flex-wrap items-center justify-between gap-3 border-t border-[var(--color-border)]",
              "bg-[var(--color-surface-2)] px-6 py-3 backdrop-blur",
            )}
          >
            <div className="font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
              {isDirty ? (
                <span className="inline-flex items-center gap-1.5 text-[var(--color-warning)]">
                  <span className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]" />
                  unsaved changes
                </span>
              ) : (
                "all changes saved"
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
                className="brand-glow gradient-sheen"
              >
                {isSaving ? "Saving…" : "Save changes"}
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Delete dialog */}
      <Dialog
        open={confirmDelete}
        onOpenChange={(o) => (!o ? setConfirmDelete(false) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-destructive)]">
              Permanent action
            </span>
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
        "inline-flex h-7 items-center gap-1 rounded-full bg-[var(--color-surface-3)] px-3",
        "ring-1 ring-inset ring-[var(--color-border)]",
        "font-mono text-[10.5px] font-medium uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-surface-4)] hover:text-[var(--color-foreground)]",
        disabled && "cursor-not-allowed opacity-50 hover:bg-[var(--color-surface-3)] hover:text-[var(--color-muted-foreground)]",
      )}
    >
      {icon}
      {label}
    </button>
  );
}

function PermissionTile({
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
        "group/perm relative flex items-start gap-2.5 rounded-xl border px-3 py-2.5",
        "transition-all duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        disabled ? "cursor-not-allowed opacity-75" : "cursor-pointer",
        checked
          ? "border-[oklch(from_var(--color-primary)_l_c_h_/_0.30)] bg-[var(--color-primary-soft)]"
          : !disabled
            ? "border-[var(--color-border)] bg-[var(--color-surface-3)] hover:border-[var(--color-border-strong)] hover:bg-[var(--color-surface-4)]"
            : "border-[var(--color-border)] bg-[var(--color-surface-3)]",
      )}
    >
      <input
        type="checkbox"
        className="sr-only"
        checked={checked}
        onChange={onToggle}
        disabled={disabled}
      />
      <span
        aria-hidden
        className={cn(
          "mt-0.5 grid h-4 w-4 shrink-0 place-items-center rounded border transition-all",
          checked
            ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
            : "border-[var(--color-input)] bg-transparent group-hover/perm:border-[var(--color-foreground)]/40",
        )}
      >
        {checked && <Check className="h-3 w-3" />}
      </span>
      <span className="min-w-0 flex-1">
        <span className="flex items-center gap-1.5">
          <span
            className={cn(
              "truncate font-mono text-[11.5px] font-medium tracking-[-0.005em]",
              checked ? "text-[var(--color-primary)]" : "text-[var(--color-foreground)]",
            )}
          >
            {perm.action}
          </span>
          {perm.isBasic && (
            <span className="rounded-full bg-[oklch(from_var(--color-info)_l_c_h_/_0.16)] px-1.5 py-0.5 text-[9px] font-medium uppercase tracking-[0.12em] text-[var(--color-info)]">
              basic
            </span>
          )}
          {dirty && (
            <span
              className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]"
              aria-label="modified"
            />
          )}
        </span>
        <span
          className={cn(
            "mt-0.5 block truncate text-[12px] leading-relaxed",
            checked
              ? "text-[var(--color-foreground)]/85"
              : "text-[var(--color-muted-foreground)]",
          )}
        >
          {perm.description}
        </span>
      </span>
    </label>
  );
}

function BackLink() {
  return (
    <Link
      to="/identity/roles"
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md px-2 py-1 -ml-2 text-[12.5px]",
        "font-medium text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)] hover:bg-[var(--color-accent)]",
        "transition-colors",
      )}
    >
      <ArrowLeft className="h-3.5 w-3.5" /> All roles
    </Link>
  );
}
