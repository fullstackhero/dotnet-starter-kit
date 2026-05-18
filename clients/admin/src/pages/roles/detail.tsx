import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ArrowLeft, ShieldCheck, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  deleteRole,
  getRoleWithPermissions,
  updateRolePermissions,
  upsertRole,
  type RoleDto,
} from "@/api/roles";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  PageHeader,
  ErrorBand,
  Field,
  LoadingRow,
  FormShell,
  FormSection,
  FormActions,
} from "@/components/list";
import {
  PERMISSION_CATALOG,
  type PermissionGroup,
} from "@/lib/permissions";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const SYSTEM_ROLE_NAMES = new Set(["Admin", "Basic"]);

const profileSchema = z.object({
  name: z.string().trim().min(2, "At least 2 characters.").max(64),
  description: z.string().trim().max(256).optional(),
});
type ProfileValues = z.infer<typeof profileSchema>;

export function RoleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ["roles", id],
    queryFn: () => getRoleWithPermissions(id!),
    enabled: Boolean(id),
  });

  const role = query.data;
  const isSystem = role ? SYSTEM_ROLE_NAMES.has(role.name) : false;

  return (
    <div className="space-y-8">
      <PageHeader
        crumbs={[
          { label: "\\ Roles" },
          { label: role?.name ?? "…", muted: true },
        ]}
        trailing={
          role ? (
            <span className="flex items-center gap-2">
              {isSystem && (
                <Badge variant="outline" className="font-mono uppercase tracking-[0.14em]">
                  system
                </Badge>
              )}
              <span>
                {(role.permissions?.length ?? 0).toString().padStart(2, "0")} grants
              </span>
            </span>
          ) : (
            "—"
          )
        }
        title={role?.name ?? "Role"}
        description={
          role?.description ?? "Inspect and edit this role's profile and permission grants."
        }
        actions={
          <Button variant="ghost" size="sm" onClick={() => navigate("/roles")}>
            <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Registry
          </Button>
        }
      />

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : "Failed to load role."
          }
        />
      )}

      {query.isLoading && <LoadingRow label="Loading role" />}

      {role && (
        <>
          <ProfileSection role={role} disabled={isSystem} />
          <PermissionEditor role={role} disabled={false /* allow editing on all roles */} />
          {!isSystem && (
            <DangerZone
              role={role}
              onDeleted={() => {
                queryClient.invalidateQueries({ queryKey: ["roles"] });
                navigate("/roles");
              }}
            />
          )}
        </>
      )}
    </div>
  );
}

// ─── Profile section ────────────────────────────────────────────────────

function ProfileSection({ role, disabled }: { role: RoleDto; disabled: boolean }) {
  const queryClient = useQueryClient();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      name: role.name,
      description: role.description ?? "",
    },
  });

  // Re-sync the form when the upstream role changes (after save).
  useEffect(() => {
    reset({ name: role.name, description: role.description ?? "" });
  }, [role, reset]);

  const mutation = useMutation({
    mutationFn: (values: ProfileValues) =>
      upsertRole({
        id: role.id,
        name: values.name,
        description: values.description?.trim() ? values.description : null,
      }),
    onSuccess: (result) => {
      toast.success("Role updated");
      queryClient.invalidateQueries({ queryKey: ["roles"] });
      queryClient.invalidateQueries({ queryKey: ["roles", result.id] });
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Update failed", { description: detail });
    },
  });

  const submitting = isSubmitting || mutation.isPending;

  return (
    <form onSubmit={handleSubmit((v) => mutation.mutate(v))}>
      <FormShell>
        <FormSection
          title="Profile"
          description={
            <>
              Name and description shown to operators when assigning users to this role.
              {disabled && (
                <span className="mt-1 block text-[var(--color-warning)]">
                  System roles cannot be renamed.
                </span>
              )}
            </>
          }
        >
          <Field id="name" label="Name" required error={errors.name?.message}>
            <Input
              id="name"
              aria-invalid={errors.name ? true : undefined}
              disabled={disabled}
              {...register("name")}
            />
          </Field>
          <Field id="description" label="Description" error={errors.description?.message}>
            <Input
              id="description"
              aria-invalid={errors.description ? true : undefined}
              {...register("description")}
            />
          </Field>
        </FormSection>

        <FormActions>
          <Button type="submit" disabled={!isDirty || submitting || disabled}>
            {submitting ? "Saving…" : "Save profile"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => reset()}
            disabled={!isDirty || submitting}
          >
            Reset
          </Button>
        </FormActions>
      </FormShell>
    </form>
  );
}

// ─── Permission editor ──────────────────────────────────────────────────

function PermissionEditor({ role, disabled }: { role: RoleDto; disabled: boolean }) {
  const queryClient = useQueryClient();
  const initial = useMemo(() => new Set(role.permissions ?? []), [role.permissions]);
  const [selected, setSelected] = useState<Set<string>>(initial);

  useEffect(() => setSelected(new Set(role.permissions ?? [])), [role.permissions]);

  const mutation = useMutation({
    mutationFn: () =>
      updateRolePermissions({
        roleId: role.id,
        permissions: Array.from(selected),
      }),
    onSuccess: () => {
      toast.success("Permissions updated");
      queryClient.invalidateQueries({ queryKey: ["roles", role.id] });
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Update failed", { description: detail });
    },
  });

  const total = useMemo(
    () => PERMISSION_CATALOG.reduce((sum, g) => sum + g.entries.length, 0),
    [],
  );
  const dirty = useMemo(() => !sameSet(selected, initial), [selected, initial]);

  const toggle = (name: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });
  };

  const toggleGroup = (group: PermissionGroup, value: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev);
      for (const e of group.entries) {
        if (value) next.add(e.name);
        else next.delete(e.name);
      }
      return next;
    });
  };

  return (
    <FormShell>
      <FormSection
        title="Permissions"
        description={
          <>
            Pick what holders of this role can do. Root-only permissions are visually
            marked — they take effect only on roles assigned in the root tenant.
            <span className="mt-2 block font-mono text-[10.5px] uppercase tracking-[0.18em]">
              {String(selected.size).padStart(2, "0")} of {String(total).padStart(2, "0")} granted
            </span>
          </>
        }
      >
        <div className="space-y-6">
        {PERMISSION_CATALOG.map((group) => {
          const groupCount = group.entries.filter((e) => selected.has(e.name)).length;
          const allOn = groupCount === group.entries.length;
          const someOn = groupCount > 0 && groupCount < group.entries.length;
          return (
            <div key={group.category} className="card-shell px-5 py-4">
              <div className="flex flex-wrap items-baseline justify-between gap-3 border-b border-[var(--color-border)] pb-3">
                <div className="min-w-0">
                  <div className="flex items-baseline gap-3">
                    <h3 className="font-display text-lg font-medium tracking-tight">
                      {group.category}
                    </h3>
                    <span className="font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                      {String(groupCount).padStart(2, "0")} / {String(group.entries.length).padStart(2, "0")}
                    </span>
                  </div>
                  <p className="mt-0.5 text-xs text-[var(--color-muted-foreground)]">
                    {group.blurb}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={disabled}
                  onClick={() => toggleGroup(group, !allOn)}
                  className="text-xs"
                >
                  {allOn ? "Clear all" : someOn ? "Select remaining" : "Select all"}
                </Button>
              </div>

              <ul className="grid grid-cols-1 gap-0 sm:grid-cols-2 sm:gap-x-6">
                {group.entries.map((entry) => {
                  const checked = selected.has(entry.name);
                  return (
                    <li key={entry.name} className="border-b border-dashed border-[var(--color-border)] py-2 last:border-0">
                      <label
                        className={cn(
                          "flex cursor-pointer items-start gap-3 rounded-md px-1.5 py-1.5 transition-colors",
                          disabled && "cursor-not-allowed opacity-60",
                          !disabled && "hover:bg-[var(--color-muted)]/60",
                        )}
                      >
                        <input
                          type="checkbox"
                          checked={checked}
                          disabled={disabled}
                          onChange={() => toggle(entry.name)}
                          className="mt-1 h-4 w-4 cursor-pointer accent-[var(--color-accent-signal)]"
                        />
                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-baseline gap-2">
                            <span className="text-sm font-medium">{entry.description}</span>
                            {entry.root && (
                              <Badge
                                variant="warning"
                                className="font-mono uppercase tracking-[0.14em]"
                              >
                                root
                              </Badge>
                            )}
                            {entry.basic && (
                              <Badge
                                variant="muted"
                                className="font-mono uppercase tracking-[0.14em]"
                              >
                                basic
                              </Badge>
                            )}
                          </div>
                          <div className="mt-0.5 truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
                            {entry.name}
                          </div>
                        </div>
                      </label>
                    </li>
                  );
                })}
              </ul>
            </div>
          );
        })}

          <div className="sticky bottom-4 z-10 flex items-center justify-between gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-4 py-3 shadow-[var(--shadow-card)]">
            <div className="meta text-[var(--color-muted-foreground)]">
              {dirty ? (
                <span className="text-[var(--color-warning)]">unsaved changes</span>
              ) : (
                "all changes saved"
              )}
            </div>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!dirty || mutation.isPending}
                onClick={() => setSelected(new Set(initial))}
              >
                Discard
              </Button>
              <Button
                type="button"
                variant="signal"
                size="sm"
                disabled={!dirty || mutation.isPending || disabled}
                onClick={() => mutation.mutate()}
              >
                <ShieldCheck className="mr-1 h-3.5 w-3.5" />
                {mutation.isPending ? "Saving…" : "Save permissions"}
              </Button>
            </div>
          </div>
        </div>
      </FormSection>
    </FormShell>
  );
}

// ─── Danger zone ────────────────────────────────────────────────────────

function DangerZone({ role, onDeleted }: { role: RoleDto; onDeleted: () => void }) {
  const [confirm, setConfirm] = useState("");

  const mutation = useMutation({
    mutationFn: () => deleteRole(role.id),
    onSuccess: () => {
      toast.success(`Role ${role.name} deleted`);
      onDeleted();
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Delete failed", { description: detail });
    },
  });

  const ready = confirm.trim() === role.name;

  return (
    <FormShell>
      <FormSection
        title="Danger zone"
        tone="danger"
        description="Delete this role. Users assigned to it will lose every permission this role grants — this is not reversible."
      >
        <div className="space-y-4 rounded-md border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.04)] p-5">
          <div>
            <p className="text-sm font-medium">
              Type <code className="code-chip">{role.name}</code> to confirm deletion.
            </p>
            <Input
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              placeholder={role.name}
              className="mt-2 max-w-sm"
              autoComplete="off"
            />
          </div>
          <Button
            type="button"
            variant="destructive"
            disabled={!ready || mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            <Trash2 className="mr-1.5 h-3.5 w-3.5" />
            {mutation.isPending ? "Deleting…" : "Delete role"}
          </Button>
        </div>
      </FormSection>
    </FormShell>
  );
}

// ─── helpers ────────────────────────────────────────────────────────────

function sameSet<T>(a: Set<T>, b: Set<T>): boolean {
  if (a.size !== b.size) return false;
  for (const item of a) if (!b.has(item)) return false;
  return true;
}
