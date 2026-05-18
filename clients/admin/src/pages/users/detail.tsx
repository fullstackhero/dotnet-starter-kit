import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Check, Mail, ShieldCheck } from "lucide-react";
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
  PageHeader,
  ErrorBand,
  LoadingRow,
  FormShell,
  FormSection,
  FormActions,
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
    onError: (err) => toast.error("Status change failed", { description: describe(err) }),
  });

  const user = userQuery.data;
  const roles = rolesQuery.data;

  const displayName =
    [user?.firstName, user?.lastName].filter(Boolean).join(" ").trim() ||
    user?.userName ||
    user?.email ||
    id;

  return (
    <div className="space-y-8">
      <PageHeader
        crumbs={[
          { label: "\\ Users" },
          { label: user?.userName ?? user?.email ?? id, muted: true },
        ]}
        trailing={id ? `ID · ${shortId(id)}` : undefined}
        title={displayName}
        description={user?.email}
        actions={
          <Button variant="ghost" size="sm" onClick={() => navigate("/users")}>
            <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Directory
          </Button>
        }
      />

      {userQuery.isError && <ErrorBand message={describe(userQuery.error)} />}

      {userQuery.isLoading && !user && <LoadingRow label="Loading account" />}

      {user && (
        <>
          <header className="card-shell flex flex-col items-start gap-6 px-6 py-6 sm:px-8 md:flex-row md:items-center md:justify-between">
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
                  {user.userName && <code className="code-chip">@{user.userName}</code>}
                  {user.email && <span className="truncate">{user.email}</span>}
                </div>
                <div className="mt-3 flex flex-wrap items-center gap-2">
                  <Badge
                    variant={user.isActive ? "success" : "muted"}
                    className="font-mono uppercase tracking-[0.14em]"
                  >
                    {user.isActive ? "Active" : "Disabled"}
                  </Badge>
                  <Badge
                    variant={user.emailConfirmed ? "info" : "warning"}
                    className="font-mono uppercase tracking-[0.14em]"
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
              className="shrink-0"
            >
              {toggleMutation.isPending
                ? "Updating…"
                : user.isActive
                  ? "Deactivate account"
                  : "Activate account"}
            </Button>
          </header>

          <FormShell>
            <FormSection
              title="Identity"
              description="Account identifiers and contact details captured at registration."
            >
              <dl className="divide-y divide-[var(--color-border)]">
                <DetailRow label="User ID" mono>{user.id ?? "—"}</DetailRow>
                <DetailRow label="Username" mono>{user.userName ?? "—"}</DetailRow>
                <DetailRow label="Email" mono>{user.email ?? "—"}</DetailRow>
                <DetailRow label="Phone" mono>{user.phoneNumber ?? "—"}</DetailRow>
                <DetailRow label="Status">{user.isActive ? "Active" : "Disabled"}</DetailRow>
                <DetailRow label="Email confirmed">
                  {user.emailConfirmed ? "Yes" : "Pending confirmation"}
                </DetailRow>
              </dl>
            </FormSection>
          </FormShell>

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
    <div className="grid grid-cols-[10rem_1fr] items-baseline gap-4 py-2.5">
      <dt className="meta text-[var(--color-muted-foreground)]">{label}</dt>
      <dd className={cn("min-w-0 break-words text-sm", mono && "font-mono text-[0.8125rem]")}>
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
    onError: (err) => toast.error("Role update failed", { description: describe(err) }),
  });

  const onSave = () => {
    const next = roles.map<UserRoleDto>((r) => ({ ...r, enabled: !!draft[r.roleId] }));
    mutation.mutate(next);
  };
  const onDiscard = () => setDraft(original);

  return (
    <FormShell>
      <FormSection
        title="Roles"
        description={
          <>
            Tap any role to toggle. Changes are batched — review and save when ready.
            <span className="mt-2 block font-mono text-[10.5px] uppercase tracking-[0.18em]">
              {dirtyCount === 0
                ? "no pending changes"
                : `${dirtyCount} ${dirtyCount === 1 ? "change" : "changes"} pending`}
            </span>
          </>
        }
      >
        {error ? (
          <ErrorBand message={describe(error)} />
        ) : loading ? (
          <p className="meta text-[var(--color-muted-foreground)]">
            Loading<span className="caret text-[var(--color-accent-signal)]" />
          </p>
        ) : roles.length === 0 ? (
          <p className="text-sm text-[var(--color-muted-foreground)]">
            No roles defined for this tenant.
          </p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {roles.map((r) => (
              <RoleChip
                key={r.roleId}
                role={r}
                enabled={!!draft[r.roleId]}
                changed={Boolean(draft[r.roleId]) !== Boolean(original[r.roleId])}
                onToggle={() => setDraft((d) => ({ ...d, [r.roleId]: !d[r.roleId] }))}
              />
            ))}
          </div>
        )}
      </FormSection>

      {!loading && roles.length > 0 && (
        <FormActions>
          <Button onClick={onSave} disabled={dirtyCount === 0 || mutation.isPending}>
            <Check className="mr-1 h-3.5 w-3.5" />
            {mutation.isPending ? "Saving…" : "Save changes"}
          </Button>
          <Button
            variant="outline"
            onClick={onDiscard}
            disabled={dirtyCount === 0 || mutation.isPending}
          >
            Discard
          </Button>
        </FormActions>
      )}
    </FormShell>
  );
}

function RoleChip({
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
    <button
      type="button"
      onClick={onToggle}
      title={role.description ?? role.roleName}
      className={cn(
        "group inline-flex items-center gap-1.5 rounded-sm border px-2.5 py-1.5 font-mono text-xs transition-colors",
        enabled
          ? "border-[var(--color-foreground)] bg-[var(--color-foreground)] text-[var(--color-background)]"
          : "border-[var(--color-border)] text-[var(--color-foreground)] hover:bg-[var(--color-muted)]",
        changed &&
          "ring-1 ring-offset-2 ring-offset-[var(--color-background)] ring-[var(--color-accent-signal)]/60",
      )}
    >
      <ShieldCheck
        className={cn("h-3 w-3", enabled ? "" : "opacity-40 group-hover:opacity-70")}
      />
      <span className="tracking-wide">{role.roleName}</span>
    </button>
  );
}

// ─── helpers ────────────────────────────────────────────────────────────

function shortId(id: string): string {
  if (id.length <= 12) return id;
  return `${id.slice(0, 4)}…${id.slice(-4)}`;
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}
