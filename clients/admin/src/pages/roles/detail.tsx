import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ArrowLeft, Lock, Shield, ShieldCheck, Trash2 } from "lucide-react";
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
  EntityPageHeader,
  ErrorBand,
  Field,
  LoadingRow,
  SettingsSection,
} from "@/components/list";
import {
  PERMISSION_CATALOG,
  type PermissionGroup,
} from "@/lib/permissions";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const SYSTEM_ROLE_NAMES = new Set(["Admin", "Basic"]);

type TFn = (key: string, opts?: Record<string, unknown>) => string;

const makeProfileSchema = (t: TFn) =>
  z.object({
    name: z.string().trim().min(2, t("detail.validation.min2")).max(64),
    description: z.string().trim().max(256).optional(),
  });
type ProfileValues = z.infer<ReturnType<typeof makeProfileSchema>>;

export function RoleDetailPage() {
  const { t } = useTranslation("roles");
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
    <div className="space-y-6">
      <EntityPageHeader
        icon={Shield}
        title={role?.name ?? t("detail.titleFallback")}
        total={role ? (role.permissions?.length ?? 0) : null}
        unit="grant"
        description={
          role?.description ?? t("detail.descriptionFallback")
        }
      >
        {isSystem && (
          <Badge variant="outline" className="font-mono text-[10px] uppercase tracking-[0.14em]">
            <Lock className="mr-1 h-3 w-3" /> {t("system")}
          </Badge>
        )}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate("/roles")}
          className="h-9 gap-1.5 rounded-lg px-3 text-[13px]"
        >
          <ArrowLeft className="size-3.5" /> {t("detail.registry")}
        </Button>
      </EntityPageHeader>

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : t("detail.loadError")
          }
        />
      )}

      {query.isLoading && <LoadingRow label={t("detail.loading")} />}

      {isSystem && role && (
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
              {t("detail.builtin.title")}
            </p>
            <p className="mt-0.5 text-[12.5px] text-[var(--color-muted-foreground)]">
              <span className="font-mono font-medium">{role.name}</span>{t("detail.builtin.body")}
            </p>
          </div>
        </div>
      )}

      {role && (
        <>
          <ProfileSection role={role} disabled={isSystem} />
          <PermissionEditor role={role} disabled={false} />
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
  const { t } = useTranslation("roles");
  const queryClient = useQueryClient();
  const profileSchema = useMemo(() => makeProfileSchema(t), [t]);
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
      toast.success(t("detail.profile.updated"));
      queryClient.invalidateQueries({ queryKey: ["roles"] });
      queryClient.invalidateQueries({ queryKey: ["roles", result.id] });
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error(t("detail.profile.updateFailed"), { description: detail });
    },
  });

  const submitting = isSubmitting || mutation.isPending;

  return (
    <form onSubmit={handleSubmit((v) => mutation.mutate(v))}>
      <SettingsSection
        title={t("detail.profile.title")}
        icon={ShieldCheck}
        description={
          disabled
            ? t("detail.profile.descriptionSystem")
            : t("detail.profile.description")
        }
        footer={
          <div className="flex items-center gap-2">
            <Button
              type="submit"
              disabled={!isDirty || submitting || disabled}
              className="h-9 rounded-lg px-4 text-[13px]"
            >
              {submitting ? t("detail.profile.saving") : t("detail.profile.save")}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => reset()}
              disabled={!isDirty || submitting}
              className="h-9 rounded-lg px-4 text-[13px]"
            >
              {t("detail.profile.reset")}
            </Button>
          </div>
        }
      >
        <div className="grid gap-4 md:grid-cols-2">
          <Field id="name" label={t("detail.field.name")} required error={errors.name?.message}>
            <Input
              id="name"
              aria-invalid={errors.name ? true : undefined}
              disabled={disabled}
              {...register("name")}
            />
          </Field>
          <Field id="description" label={t("detail.field.description")} error={errors.description?.message}>
            <Input
              id="description"
              aria-invalid={errors.description ? true : undefined}
              {...register("description")}
            />
          </Field>
        </div>
      </SettingsSection>
    </form>
  );
}

// ─── Permission editor ──────────────────────────────────────────────────

function PermissionEditor({ role, disabled }: { role: RoleDto; disabled: boolean }) {
  const { t } = useTranslation("roles");
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
      toast.success(t("detail.perm.updated"));
      queryClient.invalidateQueries({ queryKey: ["roles", role.id] });
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error(t("detail.perm.updateFailed"), { description: detail });
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
    <SettingsSection
      title={t("detail.perm.title")}
      icon={ShieldCheck}
      description={t("detail.perm.description")}
      footer={
        !disabled ? (
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
              {dirty ? (
                <span className="inline-flex items-center gap-1.5 text-[var(--color-warning)]">
                  <span className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]" />
                  {t("detail.perm.unsaved", {
                    granted: String(selected.size).padStart(2, "0"),
                    total: String(total).padStart(2, "0"),
                  })}
                </span>
              ) : (
                <span>
                  {t("detail.perm.allSaved", {
                    granted: String(selected.size).padStart(2, "0"),
                    total: String(total).padStart(2, "0"),
                  })}
                </span>
              )}
            </div>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!dirty || mutation.isPending}
                onClick={() => setSelected(new Set(initial))}
                className="h-9 rounded-lg px-3 text-[13px]"
              >
                {t("detail.perm.discard")}
              </Button>
              <Button
                type="button"
                size="sm"
                disabled={!dirty || mutation.isPending || disabled}
                onClick={() => mutation.mutate()}
                className="h-9 rounded-lg px-3 text-[13px]"
              >
                <ShieldCheck className="mr-1 h-3.5 w-3.5" />
                {mutation.isPending ? t("detail.perm.saving") : t("detail.perm.save")}
              </Button>
            </div>
          </div>
        ) : undefined
      }
    >
      <div className="space-y-4">
        {PERMISSION_CATALOG.map((group) => {
          const groupCount = group.entries.filter((e) => selected.has(e.name)).length;
          const allOn = groupCount === group.entries.length;
          const someOn = groupCount > 0 && groupCount < group.entries.length;
          return (
            <div
              key={group.category}
              className="overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]"
            >
              <div className="flex flex-wrap items-baseline justify-between gap-3 border-b border-[var(--color-border)] bg-[var(--color-muted)]/40 px-4 py-3">
                <div className="min-w-0">
                  <div className="flex items-baseline gap-3">
                    <h3 className="text-[13px] font-semibold tracking-tight text-[var(--color-foreground)]">
                      {group.category}
                    </h3>
                    <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
                      {String(groupCount).padStart(2, "0")} / {String(group.entries.length).padStart(2, "0")}
                    </span>
                  </div>
                  <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
                    {group.blurb}
                  </p>
                </div>
                <button
                  type="button"
                  disabled={disabled}
                  onClick={() => toggleGroup(group, !allOn)}
                  className={cn(
                    "cursor-pointer rounded-full px-2.5 py-0.5 text-[10.5px] font-medium uppercase tracking-wider",
                    "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                    "transition-colors",
                    disabled && "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
                  )}
                >
                  {allOn ? t("detail.perm.clearAll") : someOn ? t("detail.perm.selectRemaining") : t("detail.perm.selectAll")}
                </button>
              </div>

              <ul className="grid grid-cols-1 divide-y divide-[var(--color-border)] sm:grid-cols-2 sm:divide-y-0">
                {group.entries.map((entry) => {
                  const checked = selected.has(entry.name);
                  return (
                    <li
                      key={entry.name}
                      className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] last:border-b-0"
                    >
                      <label
                        className={cn(
                          "flex cursor-pointer items-start gap-3 px-4 py-3 text-[12.5px] transition-colors",
                          disabled && "cursor-not-allowed opacity-60",
                          !disabled && checked && "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.04)] hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.07)]",
                          !disabled && !checked && "hover:bg-[var(--color-accent)]",
                        )}
                      >
                        {/* Custom checkbox */}
                        <span
                          aria-hidden
                          className={cn(
                            "mt-0.5 grid size-4 shrink-0 place-items-center rounded border transition-all",
                            checked
                              ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                              : "border-[var(--color-input)] bg-transparent",
                          )}
                        >
                          {checked && (
                            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" aria-hidden><polyline points="20 6 9 17 4 12"/></svg>
                          )}
                        </span>
                        <input
                          type="checkbox"
                          checked={checked}
                          disabled={disabled}
                          onChange={() => toggle(entry.name)}
                          className="sr-only"
                        />
                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-baseline gap-1.5">
                            <span
                              className={cn(
                                "text-[13px] font-medium",
                                checked ? "text-[var(--color-foreground)]" : "text-[var(--color-muted-foreground)]",
                              )}
                            >
                              {entry.description}
                            </span>
                            {entry.root && (
                              <span className="rounded-full bg-[oklch(from_var(--color-warning)_l_c_h_/_0.16)] px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-[0.12em] text-[var(--color-warning)]">
                                {t("detail.perm.rootBadge")}
                              </span>
                            )}
                            {entry.basic && (
                              <span className="rounded-full bg-[oklch(from_var(--color-info)_l_c_h_/_0.16)] px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-[0.12em] text-[var(--color-info)]">
                                {t("detail.perm.basicBadge")}
                              </span>
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
      </div>
    </SettingsSection>
  );
}

// ─── Danger zone ────────────────────────────────────────────────────────

function DangerZone({ role, onDeleted }: { role: RoleDto; onDeleted: () => void }) {
  const { t } = useTranslation("roles");
  const [confirm, setConfirm] = useState("");

  const mutation = useMutation({
    mutationFn: () => deleteRole(role.id),
    onSuccess: () => {
      toast.success(t("detail.danger.deleted", { name: role.name }));
      onDeleted();
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error(t("detail.danger.deleteFailed"), { description: detail });
    },
  });

  const ready = confirm.trim() === role.name;

  return (
    <SettingsSection
      title={t("detail.danger.title")}
      icon={Trash2}
      description={t("detail.danger.description")}
    >
      <div className="space-y-4 rounded-lg border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.04)] p-5">
        <div>
          <p className="text-[13px] font-medium text-[var(--color-foreground)]">
            {t("detail.danger.confirmPre")} <code className="rounded bg-[var(--color-muted)] px-1 py-0.5 font-mono text-[12px]">{role.name}</code> {t("detail.danger.confirmPost")}
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
          className="h-9 rounded-lg px-4 text-[13px]"
        >
          <Trash2 className="mr-1.5 h-3.5 w-3.5" />
          {mutation.isPending ? t("detail.danger.deleting") : t("detail.danger.delete")}
        </Button>
      </div>
    </SettingsSection>
  );
}

// ─── helpers ────────────────────────────────────────────────────────────

function sameSet<T>(a: Set<T>, b: Set<T>): boolean {
  if (a.size !== b.size) return false;
  for (const item of a) if (!b.has(item)) return false;
  return true;
}
