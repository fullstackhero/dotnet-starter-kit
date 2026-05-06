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
  type UserDto,
  type UserRoleDto,
} from "@/api/users";
import { Button } from "@/components/ui/button";
import { Monogram } from "@/components/monogram";
import { SectionRule } from "@/components/section-rule";
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

  return (
    <div className="space-y-8">
      <SectionRule
        crumbs={[
          { label: "\\ Users" },
          { label: user?.userName ?? user?.email ?? id, muted: true },
        ]}
        trailing={id ? `ID · ${shortId(id)}` : undefined}
      />

      <div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate("/users")}
          className="-ml-2 mb-2 font-mono text-[0.6875rem] uppercase tracking-[0.18em]"
        >
          <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Directory
        </Button>

        {userQuery.isError && (
          <ErrorPanel error={userQuery.error} />
        )}

        {user ? (
          <div className="flex flex-col items-start gap-6 md:flex-row md:items-center md:justify-between">
            <div className="flex items-center gap-5">
              <Monogram
                seed={user.id ?? user.userName ?? "user"}
                firstName={user.firstName}
                lastName={user.lastName}
                fallback={user.userName ?? user.email}
                size="lg"
              />
              <div>
                <h1 className="font-display text-4xl font-semibold tracking-tight md:text-5xl">
                  {[user.firstName, user.lastName].filter(Boolean).join(" ").trim() ||
                    user.userName ||
                    user.email ||
                    "Unnamed"}
                </h1>
                <div className="mt-1 flex flex-wrap items-baseline gap-x-4 gap-y-1 font-mono text-xs text-[var(--color-muted-foreground)]">
                  {user.userName && <span>@{user.userName}</span>}
                  {user.email && <span className="truncate">{user.email}</span>}
                </div>
                <div className="mt-3 flex flex-wrap items-center gap-3">
                  <Pill icon={<Dot active={user.isActive} />}>
                    {user.isActive ? "Active" : "Disabled"}
                  </Pill>
                  <Pill icon={<Mail className="h-3 w-3" />}>
                    {user.emailConfirmed ? "Email confirmed" : "Email pending"}
                  </Pill>
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
          </div>
        ) : userQuery.isLoading ? (
          <div className="font-mono text-xs uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            Loading account…
          </div>
        ) : null}
      </div>

      {/* Dossier */}
      {user && (
        <div className="grid gap-12 border-t border-[var(--color-border)] pt-8 md:grid-cols-[20rem_1fr]">
          <IdentitySpine user={user} />
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
      )}
    </div>
  );
}

function IdentitySpine({ user }: { user: UserDto }) {
  return (
    <div className="space-y-5">
      <div>
        <p className="font-mono text-[0.6875rem] uppercase tracking-[0.22em] text-[var(--color-foreground)] border-t border-[var(--color-foreground)] pt-3">
          \\ Identity
        </p>
      </div>
      <dl className="space-y-3 text-sm">
        <Row label="User ID" mono>
          {user.id ?? "—"}
        </Row>
        <Row label="Username" mono>
          {user.userName ?? "—"}
        </Row>
        <Row label="Email" mono>
          {user.email ?? "—"}
        </Row>
        <Row label="Phone" mono>
          {user.phoneNumber ?? "—"}
        </Row>
        <Row label="Status">{user.isActive ? "Active" : "Disabled"}</Row>
        <Row label="Email">{user.emailConfirmed ? "Confirmed" : "Pending"}</Row>
      </dl>
    </div>
  );
}

function Row({ label, children, mono }: { label: string; children: React.ReactNode; mono?: boolean }) {
  return (
    <div className="grid grid-cols-[7rem_1fr] gap-4 border-b border-[var(--color-border)] pb-2.5">
      <dt className="font-mono text-[0.6875rem] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)] pt-0.5">
        {label}
      </dt>
      <dd className={cn("min-w-0 break-words", mono && "font-mono text-[0.8125rem]")}>{children}</dd>
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

  // Sync draft → server state whenever the source-of-truth roles change.
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
    <div className="space-y-5">
      <div className="flex items-baseline justify-between border-t border-[var(--color-foreground)] pt-3">
        <p className="font-mono text-[0.6875rem] uppercase tracking-[0.22em] text-[var(--color-foreground)]">
          \\ Roles
        </p>
        {!loading && roles.length > 0 && (
          <p className="font-mono text-[0.6875rem] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            {dirtyCount === 0
              ? "No pending changes"
              : `${dirtyCount} ${dirtyCount === 1 ? "change" : "changes"} pending`}
          </p>
        )}
      </div>

      {error ? (
        <p className="text-sm text-[var(--color-destructive)]">{describe(error)}</p>
      ) : loading ? (
        <p className="font-mono text-xs uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
          Loading…
        </p>
      ) : roles.length === 0 ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">
          No roles defined for this tenant.
        </p>
      ) : (
        <>
          <p className="max-w-prose text-sm text-[var(--color-muted-foreground)]">
            Click any role to toggle. Changes are batched — review and save when ready.
          </p>

          <div className="flex flex-wrap gap-2">
            {roles.map((r) => (
              <RoleChip
                key={r.roleId}
                role={r}
                enabled={!!draft[r.roleId]}
                changed={Boolean(draft[r.roleId]) !== Boolean(original[r.roleId])}
                onToggle={() =>
                  setDraft((d) => ({ ...d, [r.roleId]: !d[r.roleId] }))
                }
              />
            ))}
          </div>

          <div className="flex items-center gap-2 pt-2">
            <Button onClick={onSave} disabled={dirtyCount === 0 || mutation.isPending}>
              <Check className="mr-1 h-3.5 w-3.5" />
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
            <Button variant="outline" onClick={onDiscard} disabled={dirtyCount === 0 || mutation.isPending}>
              Discard
            </Button>
          </div>
        </>
      )}
    </div>
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
        changed && "ring-1 ring-offset-2 ring-offset-[var(--color-background)] ring-[var(--color-foreground)]/40",
      )}
    >
      <ShieldCheck
        className={cn(
          "h-3 w-3",
          enabled ? "" : "opacity-40 group-hover:opacity-70",
        )}
      />
      <span className="tracking-wide">{role.roleName}</span>
    </button>
  );
}

function Pill({ icon, children }: { icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-[var(--color-border)] px-2.5 py-1 font-mono text-[0.6875rem] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
      {icon}
      {children}
    </span>
  );
}

function Dot({ active }: { active: boolean }) {
  return (
    <span
      aria-hidden
      className={cn(
        "h-1.5 w-1.5 rounded-full",
        active ? "bg-[var(--color-foreground)]" : "border border-[var(--color-foreground)]/40 bg-transparent",
      )}
    />
  );
}

function ErrorPanel({ error }: { error: unknown }) {
  return (
    <div className="my-4 border-l-2 border-[var(--color-destructive)] bg-[var(--color-destructive)]/5 px-4 py-3 text-sm text-[var(--color-destructive)]">
      {describe(error)}
    </div>
  );
}

function shortId(id: string): string {
  if (id.length <= 12) return id;
  return `${id.slice(0, 4)}…${id.slice(-4)}`;
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}
