import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
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

type TFn = (key: string, opts?: Record<string, unknown>) => string;

/**
 * UserSessionsCard — admin view on user-detail. Lists every session a user
 * has and lets a privileged operator revoke them individually or in bulk.
 * Hidden when the operator lacks Sessions.ViewAll.
 */
export function UserSessionsCard({ userId }: { userId: string }) {
  const { t } = useTranslation("sessions");
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
      toast.success(t("revoked"));
      queryClient.invalidateQueries({ queryKey: ["admin", "user-sessions", userId] });
    },
    onError: (err) => toast.error(t("revokeFailed"), { description: describe(err) }),
    onSettled: (_d, _e, sessionId) => clearBusy(sessionId),
  });

  const revokeAll = useMutation({
    mutationFn: () => adminRevokeAllUserSessions(userId),
    onSuccess: (data) => {
      toast.success(t("revokedCount", { count: data.revokedCount }));
      queryClient.invalidateQueries({ queryKey: ["admin", "user-sessions", userId] });
    },
    onError: (err) => toast.error(t("revokeAllFailed"), { description: describe(err) }),
  });

  if (!canView) return null;

  const sessions = query.data ?? [];
  const activeCount = sessions.filter((s) => s.isActive).length;

  return (
    <FormShell>
      <FormSection
        title={t("card.title")}
        description={t("card.description")}
      >
        {query.isError ? (
          <ErrorBand
            message={
              query.error instanceof ApiRequestError
                ? query.error.problem?.detail ?? query.error.message
                : t("loadError")
            }
          />
        ) : query.isLoading ? (
          <p className="meta text-[var(--color-muted-foreground)]">
            {t("card.loadingLabel")}<span className="caret text-[var(--color-accent-signal)]" />
          </p>
        ) : sessions.length === 0 ? (
          <p className="text-sm text-[var(--color-muted-foreground)]">
            {t("card.noneOnRecord")}
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
                  t={t}
                />
              ))}
            </ul>

            {canRevoke && activeCount > 0 && (
              <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
                <span className="meta text-[var(--color-muted-foreground)]">
                  {t("card.activeCount", { count: activeCount })}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => revokeAll.mutate()}
                  disabled={revokeAll.isPending}
                >
                  <LogOut className="mr-1.5 h-3.5 w-3.5" />
                  {revokeAll.isPending ? t("signingOut") : t("card.revokeAll")}
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
  t,
}: {
  session: UserSessionDto;
  canRevoke: boolean;
  busy: boolean;
  onRevoke: () => void;
  t: TFn;
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
        <div className="truncate text-sm font-medium">{describeDevice(session, t)}</div>
        <div className="mt-0.5 flex flex-wrap items-baseline gap-x-3 gap-y-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          <span>{session.ipAddress ?? t("unknownIp")}</span>
          <span>· {t("lastSeen", { time: formatRelative(session.lastActivityAt, t) })}</span>
        </div>
      </div>
      {session.isActive ? (
        <Badge variant="brand" className="font-mono uppercase tracking-[0.14em]">
          {t("badgeActive")}
        </Badge>
      ) : (
        <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
          {t("badgeRevoked")}
        </Badge>
      )}
      {canRevoke ? (
        <Button variant="outline" size="sm" onClick={onRevoke} disabled={busy}>
          <LogOut className="mr-1.5 h-3.5 w-3.5" />
          {busy ? t("revoking") : t("revoke")}
        </Button>
      ) : (
        <span aria-hidden />
      )}
    </li>
  );
}

function describeDevice(s: UserSessionDto, t: TFn): string {
  const browser = s.browser ?? t("unknownBrowser");
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? t("unknownOs");
  return t("deviceOn", { device: `${browser}${version}`, os });
}

function formatRelative(value: string | null | undefined, t: TFn): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const diff = Date.now() - d.getTime();
  const sec = Math.round(diff / 1000);
  if (sec < 60) return t("relative.secondsAgo", { n: sec });
  const min = Math.round(sec / 60);
  if (min < 60) return t("relative.minutesAgo", { n: min });
  const hr = Math.round(min / 60);
  if (hr < 24) return t("relative.hoursAgo", { n: hr });
  const day = Math.round(hr / 24);
  if (day < 14) return t("relative.daysAgo", { n: day });
  return d.toLocaleDateString();
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}
