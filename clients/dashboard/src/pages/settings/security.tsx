import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  AlertCircle,
  LogOut,
  MonitorSmartphone,
  ShieldCheck,
  ShieldOff,
  Smartphone,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  getMySessions,
  revokeAllOtherSessions,
  revokeSession,
  type UserSessionDto,
} from "@/api/sessions";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const dateTimeFmt = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
});

function formatTimestamp(iso?: string | null) {
  if (!iso) return "—";
  return dateTimeFmt.format(new Date(iso));
}

function describeDevice(s: UserSessionDto): string {
  const browser = s.browser ?? "Unknown browser";
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? "Unknown OS";
  return `${browser}${version} · ${os}`;
}

function deviceIcon(s: UserSessionDto) {
  const isMobile = (s.deviceType ?? "").toLowerCase().includes("mobile");
  return isMobile ? Smartphone : MonitorSmartphone;
}

export function SecuritySettings() {
  const queryClient = useQueryClient();
  const [twoFactor, setTwoFactor] = useState(false);

  const sessionsQuery = useQuery({
    queryKey: ["identity", "sessions", "me"],
    queryFn: getMySessions,
    staleTime: 30_000,
  });

  const sessions = useMemo(() => {
    const data = sessionsQuery.data ?? [];
    // Active first; current device first within active.
    return [...data].sort((a, b) => {
      if (a.isCurrentSession && !b.isCurrentSession) return -1;
      if (!a.isCurrentSession && b.isCurrentSession) return 1;
      if (a.isActive && !b.isActive) return -1;
      if (!a.isActive && b.isActive) return 1;
      return new Date(b.lastActivityAt).getTime() - new Date(a.lastActivityAt).getTime();
    });
  }, [sessionsQuery.data]);

  const revokeOne = useMutation({
    mutationFn: (id: string) => revokeSession(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
      toast.success("Session revoked");
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.message
          : "Could not revoke session.",
      );
    },
  });

  const revokeAll = useMutation({
    mutationFn: () => revokeAllOtherSessions(),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
      toast.success(`Revoked ${data.revokedCount} ${data.revokedCount === 1 ? "session" : "sessions"}`);
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.message
          : "Could not revoke sessions.",
      );
    },
  });

  const otherActiveCount = useMemo(
    () => sessions.filter((s) => s.isActive && !s.isCurrentSession).length,
    [sessions],
  );

  const sessionsError =
    sessionsQuery.error instanceof ApiRequestError
      ? sessionsQuery.error.problem?.detail ?? sessionsQuery.error.message
      : sessionsQuery.error
        ? "Failed to load sessions."
        : null;

  return (
    <div className="space-y-6 fsh-enter">
      {/* Password */}
      <Card>
        <CardHeader>
          <CardTitle>Password</CardTitle>
          <CardDescription>
            Used to sign in to this tenant. Choose a strong, unique password.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4 px-6 pb-5 pt-1">
          <div className="text-sm text-[var(--color-muted-foreground)]">
            We recommend a passphrase of 16+ characters with no reuse from other services.
          </div>
          <Button variant="outline" size="sm">Change password</Button>
        </CardContent>
      </Card>

      {/* Two-factor */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            Two-factor authentication
            {twoFactor ? (
              <Badge variant="success">enabled</Badge>
            ) : (
              <Badge variant="warning">disabled</Badge>
            )}
          </CardTitle>
          <CardDescription>
            Require a one-time code from an authenticator app on every sign-in.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4 px-6 pb-5 pt-1">
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="grid h-9 w-9 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            >
              {twoFactor ? <ShieldCheck className="h-4 w-4" /> : <ShieldOff className="h-4 w-4" />}
            </span>
            <div className="text-sm">
              {twoFactor ? "Authenticator-app codes required" : "Codes are not currently required"}
            </div>
          </div>
          <Switch
            checked={twoFactor}
            onCheckedChange={setTwoFactor}
            aria-label="Two-factor authentication"
          />
        </CardContent>
      </Card>

      {/* Active sessions — wired to the real backend. */}
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                Active sessions
                {!sessionsQuery.isLoading && (
                  <Badge variant="default">
                    {sessions.filter((s) => s.isActive).length} active
                  </Badge>
                )}
              </CardTitle>
              <CardDescription>
                Browsers and devices currently signed in to your account.
              </CardDescription>
            </div>
            <Button
              variant="outline"
              size="sm"
              disabled={otherActiveCount === 0 || revokeAll.isPending}
              onClick={() => revokeAll.mutate()}
            >
              <LogOut className="mr-1.5 h-3.5 w-3.5" />
              Sign out everywhere else
            </Button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {sessionsError && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)] flex items-start gap-2">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <span>{sessionsError}</span>
            </div>
          )}

          {sessionsQuery.isLoading ? (
            <SessionsSkeleton />
          ) : sessions.length === 0 ? (
            <div className="px-6 py-12 text-center">
              <p className="text-sm font-medium tracking-tight">No sessions tracked</p>
              <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                Session activity will appear here once you sign in from any device.
              </p>
            </div>
          ) : (
            <ul>
              {sessions.map((s, idx) => {
                const Icon = deviceIcon(s);
                const isRevoking =
                  revokeOne.isPending && revokeOne.variables === s.id;
                return (
                  <li
                    key={s.id}
                    className={cn(
                      "fsh-enter group/row flex items-center justify-between gap-4",
                      "border-t border-[var(--color-border)] px-6 py-4 first:border-t-0",
                      "transition-colors",
                      !s.isActive && "opacity-60",
                    )}
                    style={{ animationDelay: `${50 * idx}ms` }}
                  >
                    <div className="flex items-center gap-3">
                      <span
                        aria-hidden
                        className={cn(
                          "grid h-9 w-9 shrink-0 place-items-center rounded-full ring-1 ring-inset",
                          s.isCurrentSession
                            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-[var(--color-primary)]/30"
                            : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] ring-[var(--color-border)]",
                        )}
                      >
                        <Icon className="h-4 w-4" />
                      </span>
                      <div className="space-y-0.5">
                        <div className="flex flex-wrap items-center gap-2 text-sm font-medium tracking-tight">
                          {describeDevice(s)}
                          {s.isCurrentSession && <Badge variant="brand">this device</Badge>}
                          {!s.isActive && <Badge variant="outline">revoked</Badge>}
                        </div>
                        <div className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                          {s.ipAddress ?? "unknown ip"} · last activity {formatTimestamp(s.lastActivityAt)}
                          {" · expires "}{formatTimestamp(s.expiresAt)}
                        </div>
                      </div>
                    </div>
                    {s.isActive && !s.isCurrentSession && (
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={isRevoking}
                        onClick={() => revokeOne.mutate(s.id)}
                      >
                        <LogOut className="mr-1.5 h-3.5 w-3.5" />
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
    </div>
  );
}

function SessionsSkeleton() {
  return (
    <ul>
      {[0, 1].map((i) => (
        <li
          key={i}
          className="flex items-center justify-between gap-4 border-t border-[var(--color-border)] px-6 py-4 first:border-t-0"
        >
          <div className="flex items-center gap-3">
            <Skeleton className="h-9 w-9 rounded-full" />
            <div className="space-y-1.5">
              <Skeleton className="h-4 w-48" />
              <Skeleton className="h-3 w-64" />
            </div>
          </div>
          <Skeleton className="h-8 w-20" />
        </li>
      ))}
    </ul>
  );
}
