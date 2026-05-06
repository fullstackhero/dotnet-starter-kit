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
  AtSign,
  CheckCircle2,
  CircleSlash2,
  Globe,
  Hash,
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
  deleteUser,
  getUserById,
  getUserRoles,
  getUserSessionsAdmin,
  toggleUserStatus,
  type AdminUserSessionDto,
  type UserRoleDto,
} from "@/api/identity";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Avatar } from "@/components/ui/avatar";
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
import { ErrorBand } from "@/components/list";
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
  const roles = rolesQuery.data ?? [];

  // Reset pending changes when fresh data arrives
  useEffect(() => {
    setPending(new Map());
  }, [roles]);

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
        <BackLink />
        <Skeleton className="h-32 rounded-2xl" />
        <Skeleton className="h-64 rounded-2xl" />
      </div>
    );
  }

  if (userQuery.isError || !user) {
    return (
      <div className="space-y-4">
        <BackLink />
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
              radial-gradient(50% 60% at 100% 0%, oklch(0.700 0.155 195 / 0.08), transparent 65%)
            `,
          }}
        />
        <div className="relative flex flex-col gap-6 px-6 py-7 sm:px-8 sm:py-9 md:flex-row md:items-center md:justify-between md:px-10">
          <div className="flex items-center gap-5">
            <Avatar
              name={display}
              src={user.imageUrl ?? undefined}
              size="lg"
              halo
              status={user.isActive ? "online" : "offline"}
            />
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  Member · profile
                </span>
              </div>
              <h1 className="text-display mt-1 truncate text-[34px] font-semibold leading-[1.05] tracking-[-0.02em] sm:text-[38px]">
                {display}
              </h1>
              <div className="mt-2 flex flex-wrap items-center gap-1.5">
                {user.userName && (
                  <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] font-medium text-[var(--color-primary)]">
                    @{user.userName}
                  </code>
                )}
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
              </div>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2 md:justify-end">
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
          </div>
        </div>
      </section>

      <div className="grid gap-5 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        {/* Profile card */}
        <Card className="fsh-enter fsh-enter-2">
          <CardHeader>
            <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Profile · facts
            </span>
            <CardTitle className="text-[15px]">Identity card</CardTitle>
            <CardDescription>
              Read-only here. Members update their own profile from settings.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 pt-1">
            <ProfileRow icon={<UserIcon className="h-3.5 w-3.5" />} label="Username" value={user.userName ?? "—"} />
            <ProfileRow icon={<Mail className="h-3.5 w-3.5" />} label="Email" value={user.email ?? "—"} />
            <ProfileRow icon={<AtSign className="h-3.5 w-3.5" />} label="First name" value={user.firstName ?? "—"} />
            <ProfileRow icon={<AtSign className="h-3.5 w-3.5" />} label="Last name" value={user.lastName ?? "—"} />
            <ProfileRow icon={<Phone className="h-3.5 w-3.5" />} label="Phone" value={user.phoneNumber ?? "—"} />
            <ProfileRow
              icon={<Hash className="h-3.5 w-3.5" />}
              label="ID"
              value={<span className="font-mono text-[11px]">{user.id}</span>}
            />
          </CardContent>
        </Card>

        {/* Roles */}
        <Card className="fsh-enter fsh-enter-3">
          <CardHeader>
            <div className="flex items-center justify-between gap-3">
              <div>
                <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  Access · roles
                </span>
                <CardTitle className="mt-1 text-[15px]">Role assignment</CardTitle>
                <CardDescription>
                  Toggle which roles apply. Changes are staged until saved.
                </CardDescription>
              </div>
              {isDirty && (
                <Badge variant="warning" className="self-start">
                  {dirtyIds.length} pending
                </Badge>
              )}
            </div>
          </CardHeader>
          <CardContent className="px-0 pb-0 pt-1">
            {rolesQuery.isLoading ? (
              <div className="space-y-3 px-6 pb-5">
                <Skeleton className="h-12 w-full rounded-md" />
                <Skeleton className="h-12 w-full rounded-md" />
                <Skeleton className="h-12 w-full rounded-md" />
              </div>
            ) : rolesQuery.isError ? (
              <div className="px-6 pb-5">
                <ErrorBand message={describe(rolesQuery.error)} />
              </div>
            ) : roles.length === 0 ? (
              <div className="px-6 pb-5 text-sm text-[var(--color-muted-foreground)]">
                No roles defined.{" "}
                <Link to="/identity/roles" className="underline hover:text-[var(--color-foreground)]">
                  Create one
                </Link>{" "}
                to start assigning access.
              </div>
            ) : (
              <ul className="border-t border-[var(--color-border)]">
                {roles.map((role) => {
                  const isOn = effective(role);
                  const dirty =
                    role.roleId !== undefined &&
                    pending.has(role.roleId) &&
                    pending.get(role.roleId) !== role.enabled;
                  return (
                    <li
                      key={role.roleId}
                      className="flex items-center justify-between gap-3 border-b border-[var(--color-border)] px-6 py-3.5 last:border-b-0 transition-colors hover:bg-[var(--color-surface-4)]"
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
          </CardContent>
          {roles.length > 0 && (
            <div
              className={cn(
                "flex items-center justify-end gap-2 border-t border-[var(--color-border)] px-6 py-3",
                "bg-[var(--color-surface-2)]",
              )}
            >
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
                className="brand-glow gradient-sheen"
              >
                {saveRoles.isPending ? "Saving…" : "Save changes"}
              </Button>
            </div>
          )}
        </Card>
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
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-destructive)]">
              Permanent action
            </span>
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
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Account state
            </span>
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
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-warning)]">
              Sensitive action
            </span>
            <DialogTitle>Impersonate {display}?</DialogTitle>
            <DialogDescription>
              You'll act as this user across the dashboard. Every action you take will be
              attributed to them in audit logs (with your operator id preserved as the actor).
              End impersonation from the banner at the top of the page when you're done.
            </DialogDescription>
          </DialogHeader>
          <div className="px-6 pb-2">
            <label className="block">
              <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
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
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-destructive)]">
              Disruptive action
            </span>
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

function BackLink() {
  return (
    <Link
      to="/identity/users"
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md px-2 py-1 -ml-2 text-[12.5px]",
        "font-medium text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)] hover:bg-[var(--color-accent)]",
        "transition-colors",
      )}
    >
      <ArrowLeft className="h-3.5 w-3.5" /> All users
    </Link>
  );
}

function ProfileRow({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-start gap-3 border-b border-[var(--color-border)] pb-3 last:border-b-0 last:pb-0">
      <div className="mt-0.5 grid h-6 w-6 place-items-center rounded-md bg-[var(--color-muted)] text-[var(--color-muted-foreground)]">
        {icon}
      </div>
      <div className="min-w-0 flex-1">
        <div className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
          {label}
        </div>
        <div className="mt-0.5 truncate text-[13px]">{value}</div>
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
    <Card className="fsh-enter fsh-enter-4">
      <CardHeader>
        <div className="flex items-center justify-between gap-3">
          <div>
            <span className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Devices · sessions
            </span>
            <CardTitle className="mt-1 text-[15px]">Active sessions</CardTitle>
            <CardDescription>
              {isLoading
                ? "Loading sessions…"
                : `${activeCount} active · ${ordered.length} total recorded`}
            </CardDescription>
          </div>
          {canRevoke && activeCount > 0 && (
            <Button variant="outline" size="sm" onClick={onRevokeAll} className="gap-1.5">
              <XCircle className="h-3.5 w-3.5" /> Revoke all
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent className="px-0 pb-0 pt-1">
        {isLoading ? (
          <div className="space-y-3 px-6 pb-5">
            <Skeleton className="h-14 w-full rounded-md" />
            <Skeleton className="h-14 w-full rounded-md" />
          </div>
        ) : isError ? (
          <div className="px-6 pb-5">
            <ErrorBand message={describe(error)} />
          </div>
        ) : ordered.length === 0 ? (
          <div className="px-6 pb-5 text-sm text-[var(--color-muted-foreground)]">
            No sessions on file. The user hasn't signed in recently.
          </div>
        ) : (
          <ul className="border-t border-[var(--color-border)]">
            {ordered.map((session) => {
              const DIcon = deviceIcon(session);
              const isRevoking = revokingId === session.id;
              return (
                <li
                  key={session.id}
                  className={cn(
                    "flex items-center gap-3 border-b border-[var(--color-border)] px-6 py-3.5 last:border-b-0",
                    "transition-colors hover:bg-[var(--color-surface-4)]",
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
                      <span className="font-mono text-[11px]">
                        last seen {sessionDateFmt.format(new Date(session.lastActivityAt))}
                      </span>
                      <span className="font-mono text-[11px] opacity-70">
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
      </CardContent>
    </Card>
  );
}
