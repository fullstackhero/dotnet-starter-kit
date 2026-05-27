import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  LogOut,
  Monitor,
  MoreHorizontal,
  MonitorSmartphone,
  Smartphone,
} from "lucide-react";
import { toast } from "sonner";
import {
  getMySessions,
  revokeAllMySessions,
  revokeMySession,
  type UserSessionDto,
} from "@/api/sessions";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { ErrorBand, LoadingRow, SettingsSection } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

export function SessionsSettings() {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["identity", "sessions", "me"],
    queryFn: getMySessions,
    staleTime: 15_000,
  });

  const sorted = useMemo(() => sortSessions(query.data ?? []), [query.data]);
  const activeOtherCount = sorted.filter((s) => s.isActive && !s.isCurrentSession).length;
  // A Set (not a single id) so concurrent revokes track independently and the
  // first to resolve doesn't clear a still-pending row's busy state.
  const [busyIds, setBusyIds] = useState<ReadonlySet<string>>(() => new Set());

  const revokeOne = useMutation({
    mutationFn: (sessionId: string) => revokeMySession(sessionId),
    onMutate: (sessionId) => setBusyIds((prev) => new Set(prev).add(sessionId)),
    onSuccess: () => {
      toast.success("Session revoked");
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
    },
    onError: (err) => toast.error("Revoke failed", { description: describe(err) }),
    onSettled: (_d, _e, sessionId) =>
      setBusyIds((prev) => {
        const next = new Set(prev);
        next.delete(sessionId);
        return next;
      }),
  });

  const revokeAll = useMutation({
    mutationFn: revokeAllMySessions,
    onSuccess: (data) => {
      toast.success(
        `Revoked ${data.revokedCount} other ${data.revokedCount === 1 ? "session" : "sessions"}`,
      );
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
    },
    onError: (err) => toast.error("Revoke all failed", { description: describe(err) }),
  });

  if (query.isLoading) return <LoadingRow label="Loading sessions" />;
  if (query.isError) {
    return (
      <ErrorBand
        message={
          query.error instanceof ApiRequestError
            ? (query.error.problem?.detail ?? query.error.message)
            : "Failed to load sessions."
        }
      />
    );
  }

  return (
    <div className="space-y-5 fsh-enter">
      <SettingsSection
        title="Active sessions"
        icon={MonitorSmartphone}
        description="Every browser or device currently signed into your account. Revoking a session signs that device out within ~10 seconds."
        footer={
          activeOtherCount > 0 ? (
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-start gap-2 text-xs">
                <AlertTriangle
                  className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[var(--color-warning)]"
                  aria-hidden
                />
                <span className="text-[var(--color-muted-foreground)]">
                  {activeOtherCount} other{" "}
                  {activeOtherCount === 1 ? "session is" : "sessions are"} active.
                  Sign them all out at once if you suspect an account compromise.
                </span>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => revokeAll.mutate()}
                disabled={revokeAll.isPending}
              >
                <LogOut className="mr-1.5 h-3.5 w-3.5" />
                {revokeAll.isPending ? "Signing out…" : "Sign out everywhere else"}
              </Button>
            </div>
          ) : undefined
        }
      >
        {sorted.length === 0 ? (
          <p className="text-sm text-[var(--color-muted-foreground)]">
            No active sessions found. (Including this one? That would be a bug — please refresh.)
          </p>
        ) : (
          <ul className="divide-y divide-[var(--color-border)]">
            {sorted.map((s) => (
              <SessionRow
                key={s.id}
                session={s}
                busy={busyIds.has(s.id)}
                onRevoke={() => revokeOne.mutate(s.id)}
              />
            ))}
          </ul>
        )}
      </SettingsSection>
    </div>
  );
}

function SessionRow({
  session,
  busy,
  onRevoke,
}: {
  session: UserSessionDto;
  busy: boolean;
  onRevoke: () => void;
}) {
  const isMobile = (session.deviceType ?? "").toLowerCase().includes("mobile");
  const Icon = isMobile ? Smartphone : Monitor;

  return (
    <li
      className={cn(
        "grid grid-cols-[auto_1fr_auto] items-center gap-4 py-3",
        !session.isActive && "opacity-60",
      )}
    >
      <span
        className={cn(
          "grid h-9 w-9 place-items-center rounded-full ring-1 ring-inset shrink-0",
          session.isCurrentSession
            ? "bg-[var(--color-primary-soft,oklch(from_var(--color-accent-signal)_l_c_h_/_0.12))] text-[var(--color-accent-signal)] ring-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.30)]"
            : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] ring-[var(--color-border)]",
        )}
      >
        <Icon className="h-4 w-4" />
      </span>
      <div className="min-w-0">
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="truncate text-sm font-medium">{describeDevice(session)}</span>
          {session.isCurrentSession && (
            <Badge variant="brand" className="font-mono uppercase tracking-[0.14em]">
              This device
            </Badge>
          )}
          {!session.isActive && (
            <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
              Revoked
            </Badge>
          )}
        </div>
        <div className="mt-0.5 flex flex-wrap items-baseline gap-x-3 gap-y-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          <span>{session.ipAddress ?? "unknown ip"}</span>
          <span>· last seen {formatRelative(session.lastActivityAt)}</span>
          <span>· expires {formatDate(session.expiresAt)}</span>
        </div>
      </div>
      {session.isCurrentSession ? (
        <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]/60 flex items-center gap-1">
          <MoreHorizontal className="h-3.5 w-3.5" aria-hidden /> use Sign out
        </span>
      ) : session.isActive ? (
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

// ─── helpers ─────────────────────────────────────────────────────────────

function sortSessions(rows: UserSessionDto[]): UserSessionDto[] {
  return [...rows].sort((a, b) => {
    if (a.isCurrentSession && !b.isCurrentSession) return -1;
    if (!a.isCurrentSession && b.isCurrentSession) return 1;
    if (a.isActive && !b.isActive) return -1;
    if (!a.isActive && b.isActive) return 1;
    return new Date(b.lastActivityAt).getTime() - new Date(a.lastActivityAt).getTime();
  });
}

function describeDevice(s: UserSessionDto): string {
  const browser = s.browser ?? "Unknown browser";
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? "unknown os";
  return `${browser}${version} on ${os}`;
}

function formatDate(value?: string | null): string {
  if (!value) return "—";
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString();
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
