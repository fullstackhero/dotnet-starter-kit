import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { toast } from "sonner";
import {
  CheckCircle2,
  CircleSlash2,
  Clock,
  Globe,
  Mail,
  MonitorSmartphone,
  Phone,
  Power,
  PowerOff,
  ShieldAlert,
  ShieldCheck,
  Smartphone,
  Trash2,
  User as UserIcon,
  UserCog,
  XCircle,
} from "lucide-react";
import {
  adminRevokeAllUserSessions,
  adminRevokeUserSession,
  assignUserRoles,
  confirmUserEmail,
  deleteUser,
  getUserById,
  getUserRoles,
  getUserSessionsAdmin,
  resendUserConfirmationEmail,
  toggleUserStatus,
  type AdminUserSessionDto,
  type UserRoleDto,
} from "@/api/identity";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
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
  EntityDetailMeta,
  EntityDetailSection,
  EntityDetailStat,
  ErrorBand,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

type DialogState =
  | { mode: "closed" }
  | { mode: "delete" }
  | { mode: "toggle-status" }
  | { mode: "impersonate" }
  | { mode: "revoke-all-sessions" };

function fullName(u: { firstName?: string; lastName?: string; userName?: string; email?: string }): string {
  const parts = [u.firstName, u.lastName].filter(Boolean);
  if (parts.length > 0) return parts.join(" ");
  return u.userName ?? u.email ?? "Unnamed user";
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function UserDetailPage() {
  const { userId = "" } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user: actor, beginImpersonation } = useAuth();
  const [dialog, setDialog] = useState<DialogState>({ mode: "closed" });
  const [impersonationReason, setImpersonationReason] = useState("");
  const [pending, setPending] = useState<Map<string, boolean>>(new Map());

  const canImpersonate = (actor?.permissions ?? []).includes("Permissions.Users.Impersonate");
  const canViewSessions = (actor?.permissions ?? []).includes("Permissions.Sessions.ViewAll");
  const canRevokeSessions = (actor?.permissions ?? []).includes("Permissions.Sessions.RevokeAll");
  const canConfirmEmail = (actor?.permissions ?? []).includes("Permissions.Users.ConfirmEmail");

  const userQuery = useQuery({
    queryKey: ["identity", "users", userId],
    queryFn: () => getUserById(userId),
    enabled: !!userId,
  });

  const rolesQuery = useQuery({
    queryKey: ["identity", "users", userId, "roles"],
    queryFn: () => getUserRoles(userId),
    enabled: !!userId,
  });

  const user = userQuery.data;
  const roles = useMemo(() => rolesQuery.data ?? [], [rolesQuery.data]);

  // Clear staged toggles only when navigating to a different user — NOT on
  // every `roles` array identity change. `roles` is `rolesQuery.data ?? []`,
  // so an incidental background refetch would otherwise wipe unsaved edits.
  // The deterministic post-save clear lives in saveRoles.onSuccess.
  useEffect(() => {
    setPending(new Map());
  }, [userId]);

  const effective = (role: UserRoleDto) => {
    if (!role.roleId) return role.enabled;
    return pending.has(role.roleId) ? !!pending.get(role.roleId) : role.enabled;
  };

  const dirtyIds = useMemo(() => {
    const out: string[] = [];
    for (const r of roles) {
      if (!r.roleId) continue;
      if (pending.has(r.roleId) && pending.get(r.roleId) !== r.enabled) {
        out.push(r.roleId);
      }
    }
    return out;
  }, [roles, pending]);

  const isDirty = dirtyIds.length > 0;

  const toggle = (role: UserRoleDto) => {
    if (!role.roleId) return;
    setPending((prev) => {
      const next = new Map(prev);
      const current = next.has(role.roleId!) ? !!next.get(role.roleId!) : role.enabled;
      next.set(role.roleId!, !current);
      return next;
    });
  };

  const saveRoles = useMutation({
    mutationFn: () => {
      const payload: UserRoleDto[] = roles.map((r) => ({
        roleId: r.roleId,
        roleName: r.roleName,
        description: r.description,
        enabled: effective(r),
      }));
      return assignUserRoles(userId, payload);
    },
    onSuccess: () => {
      toast.success("Roles updated", {
        description: `${dirtyIds.length} role${dirtyIds.length === 1 ? "" : "s"} changed.`,
      });
      setPending(new Map());
      void queryClient.invalidateQueries({
        queryKey: ["identity", "users", userId, "roles"],
      });
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const toggleStatus = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      return toggleUserStatus(user.id, !user.isActive);
    },
    onSuccess: () => {
      toast.success(user?.isActive ? "User deactivated" : "User reactivated");
      void queryClient.invalidateQueries({ queryKey: ["identity", "users", userId] });
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
      setDialog({ mode: "closed" });
    },
    onError: (err) => toast.error("Status change failed", { description: describe(err) }),
  });

  const confirmEmail = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      return confirmUserEmail(user.id);
    },
    onSuccess: () => {
      toast.success("Email confirmed");
      void queryClient.invalidateQueries({ queryKey: ["identity", "users", userId] });
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
    },
    onError: (err) => toast.error("Confirm email failed", { description: describe(err) }),
  });

  const resendConfirmation = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      return resendUserConfirmationEmail(user.id);
    },
    onSuccess: () => toast.success("Confirmation email sent"),
    onError: (err) => toast.error("Couldn't send confirmation email", { description: describe(err) }),
  });

  const removeUser = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      return deleteUser(user.id);
    },
    onSuccess: () => {
      toast.success("User deleted");
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
      navigate("/identity/users");
    },
    onError: (err) => {
      toast.error("Delete failed", { description: describe(err) });
      setDialog({ mode: "closed" });
    },
  });

  // Admin sessions
  const sessionsQuery = useQuery({
    queryKey: ["identity", "users", userId, "sessions"],
    queryFn: () => getUserSessionsAdmin(userId),
    enabled: !!userId && canViewSessions,
    staleTime: 15_000,
  });

  const revokeOne = useMutation({
    mutationFn: (sessionId: string) => adminRevokeUserSession(userId, sessionId),
    onSuccess: () => {
      toast.success("Session revoked");
      void queryClient.invalidateQueries({
        queryKey: ["identity", "users", userId, "sessions"],
      });
    },
    onError: (err) => toast.error("Revoke failed", { description: describe(err) }),
  });

  const revokeAll = useMutation({
    mutationFn: () => adminRevokeAllUserSessions(userId),
    onSuccess: (data) => {
      toast.success(
        `Revoked ${data.revokedCount} session${data.revokedCount === 1 ? "" : "s"}`,
      );
      void queryClient.invalidateQueries({
        queryKey: ["identity", "users", userId, "sessions"],
      });
      setDialog({ mode: "closed" });
    },
    onError: (err) => toast.error("Revoke-all failed", { description: describe(err) }),
  });

  // Impersonation
  const impersonate = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      if (!actor?.tenant) throw new Error("No tenant on current session");
      return beginImpersonation({
        targetUserId: user.id,
        targetTenantId: actor.tenant,
        reason: impersonationReason.trim() || undefined,
      });
    },
    onSuccess: () => {
      toast.success("Impersonation started", {
        description: "You're now acting as this user. Use the banner to end.",
      });
      setDialog({ mode: "closed" });
      setImpersonationReason("");
      navigate("/", { replace: true });
    },
    onError: (err) => {
      toast.error("Impersonation failed", { description: describe(err) });
    },
  });

  if (userQuery.isLoading) {
    return (
      <div className="space-y-6">
        <EntityDetailBack to="/identity/users" label="Back to users" />
        <Skeleton className="h-32 rounded-xl" />
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  }

  if (userQuery.isError || !user) {
    return (
      <div className="space-y-4">
        <EntityDetailBack to="/identity/users" label="Back to users" />
        <ErrorBand
          message={
            userQuery.error
              ? describe(userQuery.error)
              : "User not found."
          }
        />
      </div>
    );
  }

  const display = fullName(user);
  const activeRolesCount = roles.filter((r) => effective(r)).length;
  const sessions = sessionsQuery.data ?? [];
  const activeSessionsCount = sessions.filter((s) => s.isActive).length;
  const subtitleParts: string[] = [];
  if (user.userName) subtitleParts.push(`@${user.userName}`);
  if (user.email) subtitleParts.push(user.email);
  if (user.phoneNumber) subtitleParts.push(user.phoneNumber);

  return (
    <div className="space-y-5 pb-12">
      <EntityDetailBack to="/identity/users" label="Back to users" />

      <EntityDetailHero
        avatar={
          <EntityDetailAvatar
            name={display}
            src={user.imageUrl ?? undefined}
          />
        }
        title={display}
        badges={
          <>
            {user.isActive ? (
              <Badge variant="success">
                <ShieldCheck className="h-3 w-3" /> Active
              </Badge>
            ) : (
              <Badge variant="outline">
                <CircleSlash2 className="h-3 w-3" /> Inactive
              </Badge>
            )}
            {user.emailConfirmed ? (
              <Badge variant="brand">
                <CheckCircle2 className="h-3 w-3" /> Email confirmed
              </Badge>
            ) : (
              <Badge variant="warning">
                <Mail className="h-3 w-3" /> Email pending
              </Badge>
            )}
          </>
        }
        subtitle={subtitleParts.join(" · ") || "Member"}
        actions={
          <>
            {canImpersonate && user.id !== actor?.id && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setDialog({ mode: "impersonate" })}
                disabled={!user.isActive}
                title={!user.isActive ? "Cannot impersonate an inactive user" : undefined}
                className="gap-1.5"
              >
                <UserCog className="h-3.5 w-3.5" /> Impersonate
              </Button>
            )}
            {canConfirmEmail && !user.emailConfirmed && (
              <>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => resendConfirmation.mutate()}
                  disabled={resendConfirmation.isPending}
                  className="gap-1.5"
                >
                  <Mail className="h-3.5 w-3.5" />
                  {resendConfirmation.isPending ? "Sending…" : "Resend confirmation"}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => confirmEmail.mutate()}
                  disabled={confirmEmail.isPending}
                  className="gap-1.5"
                >
                  <CheckCircle2 className="h-3.5 w-3.5" />
                  {confirmEmail.isPending ? "Confirming…" : "Confirm email"}
                </Button>
              </>
            )}
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDialog({ mode: "toggle-status" })}
            >
              {user.isActive ? (
                <>
                  <PowerOff className="mr-1 h-3.5 w-3.5" /> Deactivate
                </>
              ) : (
                <>
                  <Power className="mr-1 h-3.5 w-3.5" /> Reactivate
                </>
              )}
            </Button>
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setDialog({ mode: "delete" })}
            >
              <Trash2 className="mr-1 h-3.5 w-3.5" /> Delete
            </Button>
          </>
        }
        stats={
          <>
            <EntityDetailStat
              icon={ShieldCheck}
              value={activeRolesCount}
              label={activeRolesCount === 1 ? "role" : "roles"}
              tone="primary"
            />
            {canViewSessions && (
              <EntityDetailStat
                icon={MonitorSmartphone}
                value={activeSessionsCount}
                label={activeSessionsCount === 1 ? "session" : "sessions"}
                tone={activeSessionsCount > 0 ? "success" : "default"}
              />
            )}
          </>
        }
        meta={
          <>
            {user.email && (
              <EntityDetailMeta icon={Mail} hideOnMobile>
                {user.email}
              </EntityDetailMeta>
            )}
            {user.phoneNumber && (
              <EntityDetailMeta icon={Phone} hideOnMobile>
                {user.phoneNumber}
              </EntityDetailMeta>
            )}
          </>
        }
      />

      <div className="grid gap-5 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        {/* Profile */}
        <EntityDetailSection
          title="Identity card"
          icon={UserIcon}
          description="Read-only here. Members update their own profile from settings."
        >
          <div className="space-y-3">
            <ProfileRow label="Username" value={user.userName ?? "—"} />
            <ProfileRow label="Email" value={user.email ?? "—"} />
            <ProfileRow label="First name" value={user.firstName ?? "—"} />
            <ProfileRow label="Last name" value={user.lastName ?? "—"} />
            <ProfileRow label="Phone" value={user.phoneNumber ?? "—"} />
            <ProfileRow
              label="ID"
              value={<span className="font-mono text-[11px]">{user.id}</span>}
            />
          </div>
        </EntityDetailSection>

        {/* Roles */}
        <EntityDetailSection
          title="Role assignment"
          icon={ShieldCheck}
          description="Toggle which roles apply. Changes are staged until saved."
          action={
            isDirty ? (
              <Badge variant="warning">{dirtyIds.length} pending</Badge>
            ) : undefined
          }
          padded={false}
          footer={
            roles.length > 0 ? (
              <div className="flex items-center justify-end gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPending(new Map())}
                  disabled={!isDirty || saveRoles.isPending}
                >
                  Discard
                </Button>
                <Button
                  size="sm"
                  onClick={() => saveRoles.mutate()}
                  disabled={!isDirty || saveRoles.isPending}
                >
                  {saveRoles.isPending ? "Saving…" : "Save changes"}
                </Button>
              </div>
            ) : undefined
          }
        >
          {rolesQuery.isLoading ? (
            <div className="space-y-3 p-5">
              <Skeleton className="h-12 w-full rounded-md" />
              <Skeleton className="h-12 w-full rounded-md" />
              <Skeleton className="h-12 w-full rounded-md" />
            </div>
          ) : rolesQuery.isError ? (
            <div className="p-5">
              <ErrorBand message={describe(rolesQuery.error)} />
            </div>
          ) : roles.length === 0 ? (
            <div className="p-5 text-sm text-[var(--color-muted-foreground)]">
              No roles defined.{" "}
              <Link to="/identity/roles" className="underline hover:text-[var(--color-foreground)]">
                Create one
              </Link>{" "}
              to start assigning access.
            </div>
          ) : (
            <ul>
              {roles.map((role) => {
                const isOn = effective(role);
                const dirty =
                  role.roleId !== undefined &&
                  pending.has(role.roleId) &&
                  pending.get(role.roleId) !== role.enabled;
                return (
                  <li
                    key={role.roleId}
                    className="flex items-center justify-between gap-3 border-b border-[var(--color-border)] px-5 py-3.5 last:border-b-0 transition-colors hover:bg-[var(--color-accent)]"
                  >
                    <div className="min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium tracking-tight">
                          {role.roleName ?? "Untitled role"}
                        </span>
                        {dirty && (
                          <span
                            className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]"
                            aria-label="modified"
                          />
                        )}
                      </div>
                      {role.description && (
                        <div className="mt-0.5 line-clamp-1 text-[12.5px] text-[var(--color-muted-foreground)]">
                          {role.description}
                        </div>
                      )}
                    </div>
                    <Switch
                      checked={isOn}
                      onCheckedChange={() => toggle(role)}
                      aria-label={`Toggle ${role.roleName ?? "role"}`}
                    />
                  </li>
                );
              })}
            </ul>
          )}
        </EntityDetailSection>
      </div>

      {/* Sessions */}
      {canViewSessions && (
        <SessionsCard
          sessions={sessionsQuery.data ?? []}
          isLoading={sessionsQuery.isLoading}
          isError={sessionsQuery.isError}
          error={sessionsQuery.error}
          canRevoke={canRevokeSessions}
          onRevoke={(id) => revokeOne.mutate(id)}
          onRevokeAll={() => setDialog({ mode: "revoke-all-sessions" })}
          revokingId={revokeOne.isPending ? revokeOne.variables : null}
        />
      )}

      {/* Delete confirmation */}
      <Dialog
        open={dialog.mode === "delete"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete this member</DialogTitle>
            <DialogDescription>
              This permanently removes{" "}
              <span className="font-medium text-[var(--color-foreground)]">{display}</span>. They
              will lose access immediately. This cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={removeUser.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => removeUser.mutate()}
              disabled={removeUser.isPending}
            >
              {removeUser.isPending ? "Deleting…" : "Delete user"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Toggle status confirmation */}
      <Dialog
        open={dialog.mode === "toggle-status"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{user.isActive ? "Deactivate user?" : "Reactivate user?"}</DialogTitle>
            <DialogDescription>
              {user.isActive
                ? `${display} will not be able to sign in until reactivated. Existing sessions remain unless revoked.`
                : `${display} will regain sign-in access immediately.`}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={toggleStatus.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button onClick={() => toggleStatus.mutate()} disabled={toggleStatus.isPending}>
              {toggleStatus.isPending
                ? "Working…"
                : user.isActive
                  ? "Deactivate"
                  : "Reactivate"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Impersonation confirmation */}
      <Dialog
        open={dialog.mode === "impersonate"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Impersonate {display}?</DialogTitle>
            <DialogDescription>
              You'll act as this user across the dashboard. Every action you take will be
              attributed to them in audit logs (with your operator id preserved as the actor).
              End impersonation from the banner at the top of the page when you're done.
            </DialogDescription>
          </DialogHeader>
          <div className="px-6 pb-2">
            <label className="block">
              <span className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
                Reason (optional, recorded in the audit log)
              </span>
              <input
                type="text"
                value={impersonationReason}
                onChange={(e) => setImpersonationReason(e.target.value)}
                placeholder="Investigating a bug report from this user…"
                maxLength={256}
                className={cn(
                  "mt-1.5 flex h-9 w-full rounded-md border border-[var(--color-input)]",
                  "bg-transparent px-3 py-1 text-sm shadow-sm",
                  "placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
                )}
              />
            </label>
          </div>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={impersonate.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              onClick={() => impersonate.mutate()}
              disabled={impersonate.isPending}
              className={cn(
                "gap-1.5",
                "bg-[var(--color-warning)] text-[var(--color-warning-foreground,white)] hover:opacity-90",
              )}
            >
              <ShieldAlert className="h-3.5 w-3.5" />
              {impersonate.isPending ? "Starting…" : "Start impersonation"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Revoke all sessions confirmation */}
      <Dialog
        open={dialog.mode === "revoke-all-sessions"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Revoke all sessions for {display}?</DialogTitle>
            <DialogDescription>
              Every active session — desktop, mobile, browser tab — will be ended immediately.
              The user will need to sign in again on each device.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={revokeAll.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => revokeAll.mutate()}
              disabled={revokeAll.isPending}
            >
              {revokeAll.isPending ? "Revoking…" : "Revoke all"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function ProfileRow({
  label,
  value,
}: {
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-baseline justify-between gap-3 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] pb-2.5 last:border-b-0 last:pb-0">
      <div className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
        {label}
      </div>
      <div className="min-w-0 truncate text-right text-[13px] text-[var(--color-foreground)]">
        {value}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Sessions card
// ───────────────────────────────────────────────────────────────────────

const sessionDateFmt = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
});

function describeDevice(s: AdminUserSessionDto): string {
  const browser = s.browser ?? "Unknown browser";
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? "Unknown OS";
  return `${browser}${version} · ${os}`;
}

function deviceIcon(s: AdminUserSessionDto) {
  const isMobile = (s.deviceType ?? "").toLowerCase().includes("mobile");
  return isMobile ? Smartphone : MonitorSmartphone;
}

function SessionsCard({
  sessions,
  isLoading,
  isError,
  error,
  canRevoke,
  onRevoke,
  onRevokeAll,
  revokingId,
}: {
  sessions: AdminUserSessionDto[];
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  canRevoke: boolean;
  onRevoke: (sessionId: string) => void;
  onRevokeAll: () => void;
  revokingId: string | null | undefined;
}) {
  const ordered = [...sessions].sort(
    (a, b) =>
      new Date(b.lastActivityAt).getTime() - new Date(a.lastActivityAt).getTime(),
  );
  const activeCount = ordered.filter((s) => s.isActive).length;

  return (
    <EntityDetailSection
      title="Active sessions"
      icon={MonitorSmartphone}
      description={
        isLoading
          ? "Loading sessions…"
          : `${activeCount} active · ${ordered.length} total recorded`
      }
      action={
        canRevoke && activeCount > 0 ? (
          <Button variant="outline" size="sm" onClick={onRevokeAll} className="gap-1.5">
            <XCircle className="h-3.5 w-3.5" /> Revoke all
          </Button>
        ) : undefined
      }
      padded={false}
    >
      {isLoading ? (
        <div className="space-y-3 p-5">
          <Skeleton className="h-14 w-full rounded-md" />
          <Skeleton className="h-14 w-full rounded-md" />
        </div>
      ) : isError ? (
        <div className="p-5">
          <ErrorBand message={describe(error)} />
        </div>
      ) : ordered.length === 0 ? (
        <div className="p-5 text-sm text-[var(--color-muted-foreground)]">
          No sessions on file. The user hasn't signed in recently.
        </div>
      ) : (
        <ul>
          {ordered.map((session) => {
            const DIcon = deviceIcon(session);
            const isRevoking = revokingId === session.id;
            return (
              <li
                key={session.id}
                className={cn(
                  "flex items-center gap-3 border-b border-[var(--color-border)] px-5 py-3.5 last:border-b-0",
                  "transition-colors hover:bg-[var(--color-accent)]",
                  !session.isActive && "opacity-60",
                )}
              >
                <span
                  aria-hidden
                  className={cn(
                    "grid h-9 w-9 shrink-0 place-items-center rounded-lg",
                    session.isActive
                      ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                      : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
                  )}
                >
                  <DIcon className="h-4 w-4" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="truncate text-sm font-medium tracking-tight">
                      {describeDevice(session)}
                    </span>
                    {session.isActive ? (
                      <Badge variant="success">Active</Badge>
                    ) : (
                      <Badge variant="outline">Ended</Badge>
                    )}
                  </div>
                  <div className="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[12px] text-[var(--color-muted-foreground)]">
                    {session.ipAddress && (
                      <span className="inline-flex items-center gap-1 font-mono">
                        <Globe className="h-3 w-3" /> {session.ipAddress}
                      </span>
                    )}
                    <span className="inline-flex items-center gap-1">
                      <Clock className="h-3 w-3" /> last seen{" "}
                      {sessionDateFmt.format(new Date(session.lastActivityAt))}
                    </span>
                    <span className="opacity-70">
                      started {sessionDateFmt.format(new Date(session.createdAt))}
                    </span>
                  </div>
                </div>
                {canRevoke && session.isActive && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => onRevoke(session.id)}
                    disabled={isRevoking}
                    className="shrink-0 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"
                  >
                    <XCircle className="mr-1 h-3.5 w-3.5" />
                    {isRevoking ? "Revoking…" : "Revoke"}
                  </Button>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </EntityDetailSection>
  );
}
