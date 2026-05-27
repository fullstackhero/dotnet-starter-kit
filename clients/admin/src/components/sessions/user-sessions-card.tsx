import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { LogOut, Monitor, Smartphone } from "lucide-react";
import { toast } from "sonner";
import {
  adminRevokeAllUserSessions,
  adminRevokeUserSession,
  getUserSessions,
  type UserSessionDto,
} from "@/api/sessions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  ErrorBand,
  FormSection,
  FormShell,
} from "@/components/list";
import { IdentityPermissions } from "@/lib/permissions";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * UserSessionsCard — admin view on user-detail. Lists every session a user
 * has and lets a privileged operator revoke them individually or in bulk.
 * Hidden when the operator lacks Sessions.ViewAll.
 */
export function UserSessionsCard({ userId }: { userId: string }) {
  const { user } = useAuth();
  const granted = user?.permissions ?? [];
  const canView = granted.includes(IdentityPermissions.Sessions.ViewAll);
  const canRevoke = granted.includes(IdentityPermissions.Sessions.RevokeAll);
  const queryClient = useQueryClient();
  // A Set (not a single id) so two quick revokes track independently and the
  // first to resolve doesn't clear the still-pending second row's busy state.
  const [busyIds, setBusyIds] = useState<ReadonlySet<string>>(() => new Set());
  const addBusy = (id: string) =>
    setBusyIds((prev) => new Set(prev).add(id));
  const clearBusy = (id: string) =>
    setBusyIds((prev) => {
      const next = new Set(prev);
      next.delete(id);
      return next;
    });

  const query = useQuery({
    queryKey: ["admin", "user-sessions", userId],
    queryFn: () => getUserSessions(userId),
    enabled: canView && Boolean(userId),
    staleTime: 15_000,
  });

  const revokeOne = useMutation({
    mutationFn: (sessionId: string) => adminRevokeUserSession(userId, sessionId),
    onMutate: (sessionId) => addBusy(sessionId),
    onSuccess: () => {
      toast.success("Session revoked");
      queryClient.invalidateQueries({ queryKey: ["admin", "user-sessions", userId] });
    },
    onError: (err) => toast.error("Revoke failed", { description: describe(err) }),
    onSettled: (_d, _e, sessionId) => clearBusy(sessionId),
  });

  const revokeAll = useMutation({
    mutationFn: () => adminRevokeAllUserSessions(userId),
    onSuccess: (data) => {
      toast.success(`Revoked ${data.revokedCount} ${data.revokedCount === 1 ? "session" : "sessions"}`);
      queryClient.invalidateQueries({ queryKey: ["admin", "user-sessions", userId] });
    },
    onError: (err) => toast.error("Revoke all failed", { description: describe(err) }),
  });

  if (!canView) return null;

  const sessions = query.data ?? [];
  const activeCount = sessions.filter((s) => s.isActive).length;

  return (
    <FormShell>
      <FormSection
        title="Sessions"
        description="Active browser/device sessions for this user. Revoking signs the device out within ~10 seconds."
      >
        {query.isError ? (
          <ErrorBand
            message={
              query.error instanceof ApiRequestError
                ? query.error.problem?.detail ?? query.error.message
                : "Failed to load sessions."
            }
          />
        ) : query.isLoading ? (
          <p className="meta text-[var(--color-muted-foreground)]">
            Loading<span className="caret text-[var(--color-accent-signal)]" />
          </p>
        ) : sessions.length === 0 ? (
          <p className="text-sm text-[var(--color-muted-foreground)]">
            No sessions on record for this user.
          </p>
        ) : (
          <>
            <ul className="divide-y divide-[var(--color-border)] border-y border-[var(--color-border)]">
              {sessions.map((s) => (
                <SessionRow
                  key={s.id}
                  session={s}
                  canRevoke={canRevoke && s.isActive}
                  busy={busyIds.has(s.id)}
                  onRevoke={() => revokeOne.mutate(s.id)}
                />
              ))}
            </ul>

            {canRevoke && activeCount > 0 && (
              <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
                <span className="meta text-[var(--color-muted-foreground)]">
                  {activeCount} active {activeCount === 1 ? "session" : "sessions"}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => revokeAll.mutate()}
                  disabled={revokeAll.isPending}
                >
                  <LogOut className="mr-1.5 h-3.5 w-3.5" />
                  {revokeAll.isPending ? "Signing out…" : "Revoke all sessions"}
                </Button>
              </div>
            )}
          </>
        )}
      </FormSection>
    </FormShell>
  );
}

function SessionRow({
  session,
  canRevoke,
  busy,
  onRevoke,
}: {
  session: UserSessionDto;
  canRevoke: boolean;
  busy: boolean;
  onRevoke: () => void;
}) {
  const Icon = (session.deviceType ?? "").toLowerCase().includes("mobile") ? Smartphone : Monitor;
  return (
    <li
      className={cn(
        "grid grid-cols-[auto_1fr_auto_auto] items-center gap-4 py-3",
        !session.isActive && "opacity-60",
      )}
    >
      <Icon className="h-4 w-4 text-[var(--color-muted-foreground)]" />
      <div className="min-w-0">
        <div className="truncate text-sm font-medium">{describeDevice(session)}</div>
        <div className="mt-0.5 flex flex-wrap items-baseline gap-x-3 gap-y-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          <span>{session.ipAddress ?? "unknown ip"}</span>
          <span>· last seen {formatRelative(session.lastActivityAt)}</span>
        </div>
      </div>
      {session.isActive ? (
        <Badge variant="brand" className="font-mono uppercase tracking-[0.14em]">
          Active
        </Badge>
      ) : (
        <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
          Revoked
        </Badge>
      )}
      {canRevoke ? (
        <Button variant="outline" size="sm" onClick={onRevoke} disabled={busy}>
          <LogOut className="mr-1.5 h-3.5 w-3.5" />
          {busy ? "Revoking…" : "Revoke"}
        </Button>
      ) : (
        <span aria-hidden />
      )}
    </li>
  );
}

function describeDevice(s: UserSessionDto): string {
  const browser = s.browser ?? "Unknown browser";
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? "unknown os";
  return `${browser}${version} on ${os}`;
}

function formatRelative(value?: string | null): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const diff = Date.now() - d.getTime();
  const sec = Math.round(diff / 1000);
  if (sec < 60) return `${sec}s ago`;
  const min = Math.round(sec / 60);
  if (min < 60) return `${min}m ago`;
  const hr = Math.round(min / 60);
  if (hr < 24) return `${hr}h ago`;
  const day = Math.round(hr / 24);
  if (day < 14) return `${day}d ago`;
  return d.toLocaleDateString();
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}
