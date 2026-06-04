import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Check, Mail, ShieldCheck, User as UserIcon, Users } from "lucide-react";
import { toast } from "sonner";
import {
  assignUserRoles,
  getUser,
  getUserRoles,
  toggleUserStatus,
  type UserRoleDto,
} from "@/api/users";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Monogram } from "@/components/monogram";
import { UserSessionsCard } from "@/components/sessions/user-sessions-card";
import {
  EntityPageHeader,
  ErrorBand,
  LoadingRow,
  SettingsSection,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

export function UserDetailPage() {
  const { id = "" } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const userQuery = useQuery({
    queryKey: ["user", id],
    queryFn: () => getUser(id),
    enabled: !!id,
  });

  const rolesQuery = useQuery({
    queryKey: ["user", id, "roles"],
    queryFn: () => getUserRoles(id),
    enabled: !!id,
  });

  const toggleMutation = useMutation({
    mutationFn: (activate: boolean) => toggleUserStatus(id, activate),
    onSuccess: (_, activate) => {
      toast.success(activate ? "User activated" : "User deactivated");
      queryClient.invalidateQueries({ queryKey: ["user", id] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err) => toast.error("Status change failed", { description: describeErr(err) }),
  });

  const user = userQuery.data;
  const roles = rolesQuery.data;

  const displayName =
    [user?.firstName, user?.lastName].filter(Boolean).join(" ").trim() ||
    user?.userName ||
    user?.email ||
    id;

  return (
    <div className="space-y-6">
      <EntityPageHeader
        icon={Users}
        title={displayName}
        description={user?.email ?? undefined}
      >
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate("/users")}
          className="h-9 gap-1.5 rounded-lg px-3 text-[13px]"
        >
          <ArrowLeft className="size-3.5" /> Directory
        </Button>
      </EntityPageHeader>

      {userQuery.isError && <ErrorBand message={describeErr(userQuery.error)} />}

      {userQuery.isLoading && !user && <LoadingRow label="Loading account" />}

      {user && (
        <>
          {/* Hero card */}
          <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs">
            <div className="flex flex-col items-start gap-6 px-6 py-6 sm:flex-row sm:items-center sm:justify-between sm:px-8">
              <div className="flex items-center gap-5">
                <Monogram
                  seed={user.id ?? user.userName ?? "user"}
                  firstName={user.firstName ?? undefined}
                  lastName={user.lastName ?? undefined}
                  fallback={user.userName ?? user.email ?? undefined}
                  size="lg"
                />
                <div>
                  <h2 className="font-display text-2xl font-semibold tracking-tight md:text-3xl">
                    {displayName}
                  </h2>
                  <div className="mt-1 flex flex-wrap items-baseline gap-x-4 gap-y-1 font-mono text-xs text-[var(--color-muted-foreground)]">
                    {user.userName && (
                      <code className="rounded bg-[var(--color-muted)] px-1 py-0.5 text-[11px]">
                        @{user.userName}
                      </code>
                    )}
                    {user.email && <span className="truncate">{user.email}</span>}
                  </div>
                  <div className="mt-3 flex flex-wrap items-center gap-2">
                    <Badge
                      variant={user.isActive ? "success" : "muted"}
                      className="font-mono text-[10px] uppercase tracking-[0.14em]"
                    >
                      {user.isActive ? "Active" : "Disabled"}
                    </Badge>
                    <Badge
                      variant={user.emailConfirmed ? "info" : "warning"}
                      className="font-mono text-[10px] uppercase tracking-[0.14em]"
                    >
                      <Mail className="h-3 w-3" />
                      {user.emailConfirmed ? "Email confirmed" : "Email pending"}
                    </Badge>
                  </div>
                </div>
              </div>

              <Button
                variant={user.isActive ? "outline" : "default"}
                onClick={() => toggleMutation.mutate(!user.isActive)}
                disabled={toggleMutation.isPending}
                className="shrink-0 h-9 rounded-lg px-4 text-[13px]"
              >
                {toggleMutation.isPending
                  ? "Updating…"
                  : user.isActive
                    ? "Deactivate account"
                    : "Activate account"}
              </Button>
            </div>
          </div>

          <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
            {/* Identity details */}
            <SettingsSection
              title="Identity card"
              icon={UserIcon}
              description="Account identifiers and contact details captured at registration."
            >
              <dl className="space-y-0 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
                <DetailRow label="User ID" mono>
                  {user.id ?? "—"}
                </DetailRow>
                <DetailRow label="Username" mono>
                  {user.userName ?? "—"}
                </DetailRow>
                <DetailRow label="Email" mono>
                  {user.email ?? "—"}
                </DetailRow>
                <DetailRow label="Phone" mono>
                  {user.phoneNumber ?? "—"}
                </DetailRow>
                <DetailRow label="Status">
                  {user.isActive ? "Active" : "Disabled"}
                </DetailRow>
                <DetailRow label="Email confirmed">
                  {user.emailConfirmed ? "Yes" : "Pending confirmation"}
                </DetailRow>
              </dl>
            </SettingsSection>

            {/* Roles editor */}
            <RolesEditor
              userId={user.id ?? id}
              roles={roles ?? []}
              loading={rolesQuery.isLoading}
              error={rolesQuery.error}
              onSaved={() => {
                queryClient.invalidateQueries({ queryKey: ["user", id, "roles"] });
                queryClient.invalidateQueries({ queryKey: ["users"] });
              }}
            />
          </div>

          <UserSessionsCard userId={user.id ?? id} />
        </>
      )}
    </div>
  );
}

// ─── subcomponents ──────────────────────────────────────────────────────

function DetailRow({
  label,
  children,
  mono,
}: {
  label: string;
  children: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div className="flex items-baseline justify-between gap-3 py-2.5">
      <dt className="shrink-0 text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd
        className={cn(
          "min-w-0 truncate text-right text-[13px] text-[var(--color-foreground)]",
          mono && "font-mono text-[11.5px]",
        )}
      >
        {children}
      </dd>
    </div>
  );
}

function RolesEditor({
  userId,
  roles,
  loading,
  error,
  onSaved,
}: {
  userId: string;
  roles: UserRoleDto[];
  loading: boolean;
  error: unknown;
  onSaved: () => void;
}) {
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState<Record<string, boolean>>({});

  useEffect(() => {
    setDraft(Object.fromEntries(roles.map((r) => [r.roleId, r.enabled])));
  }, [roles]);

  const original = useMemo(
    () => Object.fromEntries(roles.map((r) => [r.roleId, r.enabled])),
    [roles],
  );
  const dirtyCount = useMemo(
    () =>
      Object.keys(draft).reduce(
        (acc, k) => acc + (Boolean(draft[k]) === Boolean(original[k]) ? 0 : 1),
        0,
      ),
    [draft, original],
  );

  const mutation = useMutation({
    mutationFn: (next: UserRoleDto[]) => assignUserRoles(userId, next),
    onSuccess: () => {
      toast.success("Roles updated");
      queryClient.invalidateQueries({ queryKey: ["user", userId, "roles"] });
      onSaved();
    },
    onError: (err) => toast.error("Role update failed", { description: describeErr(err) }),
  });

  const onSave = () => {
    const next = roles.map<UserRoleDto>((r) => ({ ...r, enabled: !!draft[r.roleId] }));
    mutation.mutate(next);
  };
  const onDiscard = () => setDraft(original);

  return (
    <SettingsSection
      title="Role assignment"
      icon={ShieldCheck}
      description={
        dirtyCount > 0
          ? `${dirtyCount} pending change${dirtyCount === 1 ? "" : "s"} — review and save when ready.`
          : "Tap any role to toggle. Changes are batched — review and save when ready."
      }
      footer={
        !loading && roles.length > 0 ? (
          <div className="flex items-center gap-2">
            <Button
              onClick={onSave}
              disabled={dirtyCount === 0 || mutation.isPending}
              className="h-9 rounded-lg px-4 text-[13px]"
            >
              <Check className="mr-1 h-3.5 w-3.5" />
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
            <Button
              variant="outline"
              onClick={onDiscard}
              disabled={dirtyCount === 0 || mutation.isPending}
              className="h-9 rounded-lg px-4 text-[13px]"
            >
              Discard
            </Button>
          </div>
        ) : undefined
      }
    >
      {error ? (
        <ErrorBand message={describeErr(error)} />
      ) : loading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">
          Loading
          <span className="caret text-[var(--color-accent-signal)]" />
        </p>
      ) : roles.length === 0 ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">
          No roles defined for this tenant.
        </p>
      ) : (
        <ul className="divide-y divide-[var(--color-border)]">
          {roles.map((r) => (
            <RoleRow
              key={r.roleId}
              role={r}
              enabled={!!draft[r.roleId]}
              changed={Boolean(draft[r.roleId]) !== Boolean(original[r.roleId])}
              onToggle={() => setDraft((d) => ({ ...d, [r.roleId]: !d[r.roleId] }))}
            />
          ))}
        </ul>
      )}
    </SettingsSection>
  );
}

function RoleRow({
  role,
  enabled,
  changed,
  onToggle,
}: {
  role: UserRoleDto;
  enabled: boolean;
  changed: boolean;
  onToggle: () => void;
}) {
  return (
    <li className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0">
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <span className="text-[13px] font-medium tracking-tight text-[var(--color-foreground)]">
            {role.roleName ?? "Untitled role"}
          </span>
          {changed && (
            <span
              className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]"
              aria-label="modified"
            />
          )}
        </div>
        {role.description && (
          <div className="mt-0.5 line-clamp-1 text-[11.5px] text-[var(--color-muted-foreground)]">
            {role.description}
          </div>
        )}
      </div>
      <RoleChip
        enabled={enabled}
        changed={changed}
        onToggle={onToggle}
        label={role.roleName ?? "role"}
      />
    </li>
  );
}

function RoleChip({
  enabled,
  changed,
  onToggle,
  label,
}: {
  enabled: boolean;
  changed: boolean;
  onToggle: () => void;
  label: string;
}) {
  return (
    <button
      type="button"
      onClick={onToggle}
      aria-label={`Toggle ${label}`}
      className={cn(
        "inline-flex shrink-0 items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[11px] transition-colors",
        enabled
          ? "border-[var(--color-foreground)] bg-[var(--color-foreground)] text-[var(--color-background)]"
          : "border-[var(--color-border)] text-[var(--color-foreground)] hover:bg-[var(--color-muted)]",
        changed &&
          "ring-1 ring-offset-2 ring-offset-[var(--color-background)] ring-[var(--color-warning)]/60",
      )}
    >
      <ShieldCheck className={cn("h-3 w-3", enabled ? "" : "opacity-40")} />
      <span>{enabled ? "On" : "Off"}</span>
    </button>
  );
}

// ─── helpers ────────────────────────────────────────────────────────────

function describeErr(err: unknown): string {
  if (err instanceof ApiRequestError)
    return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}
