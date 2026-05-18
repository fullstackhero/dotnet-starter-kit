import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  AlertCircle,
  ClipboardCheck,
  Copy,
  KeyRound,
  LogOut,
  MonitorSmartphone,
  ShieldCheck,
  ShieldOff,
  Smartphone,
} from "lucide-react";
import QRCode from "qrcode";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  changePassword,
  disableTwoFactor,
  enrollTwoFactor,
  getMyProfile,
  verifyEnrollTwoFactor,
  type TwoFactorEnrollmentResponse,
} from "@/api/identity";
import {
  getMySessions,
  revokeAllOtherSessions,
  revokeSession,
  type UserSessionDto,
} from "@/api/sessions";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const PROFILE_KEY = ["identity", "me"] as const;

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

function apiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) {
    return err.problem?.detail ?? err.problem?.title ?? err.message;
  }
  if (err instanceof Error) return err.message;
  return fallback;
}

// ─────────────────────────────────────────────────────────────────────────
// Page
// ─────────────────────────────────────────────────────────────────────────

export function SecuritySettings() {
  const queryClient = useQueryClient();

  const profileQuery = useQuery({ queryKey: PROFILE_KEY, queryFn: getMyProfile });
  const twoFactorEnabled = profileQuery.data?.twoFactorEnabled ?? false;

  const sessionsQuery = useQuery({
    queryKey: ["identity", "sessions", "me"],
    queryFn: getMySessions,
    staleTime: 30_000,
  });

  const sessions = useMemo(() => {
    const data = sessionsQuery.data ?? [];
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
    onError: (err) =>
      toast.error(apiErrorMessage(err, "Could not revoke session.")),
  });

  const revokeAll = useMutation({
    mutationFn: () => revokeAllOtherSessions(),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
      toast.success(
        `Revoked ${data.revokedCount} ${data.revokedCount === 1 ? "session" : "sessions"}`,
      );
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, "Could not revoke sessions.")),
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
      <PasswordCard />
      <TwoFactorCard enabled={twoFactorEnabled} loading={profileQuery.isLoading} />

      {/* Active sessions — already wired to the backend. */}
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

// ─────────────────────────────────────────────────────────────────────────
// Password card — Dialog-driven change-password flow
// ─────────────────────────────────────────────────────────────────────────

function PasswordCard() {
  const [open, setOpen] = useState(false);

  return (
    <>
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
          <Button variant="outline" size="sm" onClick={() => setOpen(true)}>
            Change password
          </Button>
        </CardContent>
      </Card>

      <ChangePasswordDialog open={open} onOpenChange={setOpen} />
    </>
  );
}

function ChangePasswordDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
}) {
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);

  // Reset state every time the dialog opens so stale values don't bleed.
  useEffect(() => {
    if (open) {
      setCurrent("");
      setNext("");
      setConfirm("");
      setLocalError(null);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      changePassword({
        password: current,
        newPassword: next,
        confirmNewPassword: confirm,
      }),
    onSuccess: () => {
      toast.success("Password changed", {
        description: "Other active sessions remain valid until you revoke them.",
      });
      onOpenChange(false);
    },
    onError: (err) => setLocalError(apiErrorMessage(err, "Could not change password.")),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLocalError(null);

    if (next.length < 8) {
      setLocalError("New password must be at least 8 characters.");
      return;
    }
    if (next !== confirm) {
      setLocalError("Passwords don't match.");
      return;
    }
    if (next === current) {
      setLocalError("New password must differ from the current one.");
      return;
    }
    mutation.mutate();
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Change password</DialogTitle>
          <DialogDescription>
            Sign-out events for other devices aren't fired automatically —
            visit the Sessions list below to end them after rotating your password.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-3" noValidate>
          <div className="space-y-1.5">
            <Label htmlFor="cp-current">Current password</Label>
            <Input
              id="cp-current"
              type="password"
              autoComplete="current-password"
              value={current}
              onChange={(e) => setCurrent(e.target.value)}
              required
              autoFocus
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="cp-next">New password</Label>
            <Input
              id="cp-next"
              type="password"
              autoComplete="new-password"
              value={next}
              onChange={(e) => setNext(e.target.value)}
              required
              minLength={8}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="cp-confirm">Confirm new password</Label>
            <Input
              id="cp-confirm"
              type="password"
              autoComplete="new-password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              required
              minLength={8}
            />
          </div>

          {localError && (
            <div
              role="alert"
              className="flex items-start gap-2 rounded-md border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.40)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-sm text-[var(--color-destructive)]"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <span className="leading-snug">{localError}</span>
            </div>
          )}

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
              disabled={mutation.isPending}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={mutation.isPending || !current || !next || !confirm}
            >
              <KeyRound className="mr-1 h-3.5 w-3.5" />
              {mutation.isPending ? "Updating…" : "Update password"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Two-factor card — real enroll/verify/disable flow
// ─────────────────────────────────────────────────────────────────────────

function TwoFactorCard({ enabled, loading }: { enabled: boolean; loading: boolean }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          Two-factor authentication
          {loading ? (
            <Skeleton className="h-5 w-16 rounded-full" />
          ) : enabled ? (
            <Badge variant="success">enabled</Badge>
          ) : (
            <Badge variant="warning">disabled</Badge>
          )}
        </CardTitle>
        <CardDescription>
          Require a one-time code from an authenticator app on every sign-in.
        </CardDescription>
      </CardHeader>
      <CardContent className="px-6 pb-5 pt-1">
        {loading ? (
          <Skeleton className="h-9 w-40" />
        ) : enabled ? (
          <TwoFactorDisable />
        ) : (
          <TwoFactorEnroll />
        )}
      </CardContent>
    </Card>
  );
}

function TwoFactorEnroll() {
  const queryClient = useQueryClient();
  const [enrollment, setEnrollment] = useState<TwoFactorEnrollmentResponse | null>(null);
  const [code, setCode] = useState("");
  const [qrSvg, setQrSvg] = useState<string | null>(null);
  const [copiedKey, setCopiedKey] = useState(false);

  const beginMutation = useMutation({
    mutationFn: enrollTwoFactor,
    onSuccess: (data) => setEnrollment(data),
    onError: (err) =>
      toast.error("Enrollment failed", {
        description: apiErrorMessage(err, "Could not start enrollment."),
      }),
  });

  const verifyMutation = useMutation({
    mutationFn: (otp: string) => verifyEnrollTwoFactor(otp),
    onSuccess: (data) => {
      if (data.success) {
        toast.success("Two-factor enabled", {
          description: "Future logins require a 6-digit code from your authenticator.",
        });
        setEnrollment(null);
        setCode("");
        setQrSvg(null);
        void queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
      } else {
        toast.error("Verification failed", {
          description: "That code didn't match. Try again.",
        });
      }
    },
    onError: (err) =>
      toast.error("Verification failed", {
        description: apiErrorMessage(err, "Could not verify code."),
      }),
  });

  // Render the QR as inline SVG when the otpauth URI changes. Keeps the
  // image source-of-truth in JS without an extra <canvas>, and lets us
  // theme it via currentColor so it tracks dark/light mode.
  useEffect(() => {
    if (!enrollment) {
      setQrSvg(null);
      return;
    }
    let cancelled = false;
    void QRCode.toString(enrollment.authenticatorUri, {
      type: "svg",
      margin: 1,
      width: 200,
      errorCorrectionLevel: "M",
    }).then((svg) => {
      if (cancelled) return;
      const themed = svg
        .replace(/fill="#ffffff"/gi, 'fill="transparent"')
        .replace(/fill="#FFFFFF"/gi, 'fill="transparent"')
        .replace(/fill="#000000"/gi, 'fill="currentColor"')
        .replace(/fill="#000"/gi, 'fill="currentColor"');
      setQrSvg(themed);
    });
    return () => {
      cancelled = true;
    };
  }, [enrollment]);

  const copyKey = async () => {
    if (!enrollment) return;
    try {
      await navigator.clipboard.writeText(enrollment.sharedKey);
      setCopiedKey(true);
      window.setTimeout(() => setCopiedKey(false), 1500);
    } catch {
      /* clipboard unavailable — silently noop */
    }
  };

  if (!enrollment) {
    return (
      <div className="flex flex-wrap items-center gap-3">
        <Button
          onClick={() => beginMutation.mutate()}
          disabled={beginMutation.isPending}
        >
          <ShieldCheck className="mr-1.5 h-3.5 w-3.5" />
          {beginMutation.isPending ? "Generating…" : "Enable two-factor"}
        </Button>
        <span className="text-xs text-[var(--color-muted-foreground)]">
          You'll scan a QR code in your authenticator app
          (1Password, Google Authenticator, Authy, …).
        </span>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-5 sm:grid-cols-[14rem_1fr] sm:items-start">
        <div className="grid h-52 w-52 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-2 text-[var(--color-foreground)]">
          {qrSvg ? (
            <div
              aria-label="Two-factor QR code"
              role="img"
              className="h-full w-full [&_svg]:h-full [&_svg]:w-full"
              // eslint-disable-next-line react/no-danger
              dangerouslySetInnerHTML={{ __html: qrSvg }}
            />
          ) : (
            <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
              Rendering…
            </span>
          )}
        </div>
        <div className="space-y-3">
          <div>
            <div className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
              can't scan? enter manually
            </div>
            <div className="mt-1 flex items-center gap-2">
              <code className="break-all rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-2 py-1 font-mono text-[11px]">
                {enrollment.sharedKey}
              </code>
              <button
                type="button"
                onClick={copyKey}
                className="inline-flex h-7 items-center gap-1 rounded-md px-2 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
              >
                {copiedKey ? (
                  <>
                    <ClipboardCheck className="h-3 w-3" /> copied
                  </>
                ) : (
                  <>
                    <Copy className="h-3 w-3" /> copy
                  </>
                )}
              </button>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="totp-code">6-digit code from your app</Label>
            <Input
              id="totp-code"
              inputMode="numeric"
              autoComplete="one-time-code"
              placeholder="123 456"
              maxLength={8}
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\s/g, ""))}
              className={cn(
                "font-mono text-lg tracking-[0.4em]",
                code.length >= 6 && "border-[var(--color-primary)]",
              )}
            />
          </div>

          <div className="flex flex-wrap items-center gap-2 pt-1">
            <Button
              onClick={() => verifyMutation.mutate(code)}
              disabled={code.length < 6 || verifyMutation.isPending}
            >
              {verifyMutation.isPending ? "Verifying…" : "Confirm & enable"}
            </Button>
            <Button
              variant="ghost"
              onClick={() => {
                setEnrollment(null);
                setCode("");
              }}
              disabled={verifyMutation.isPending}
            >
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

function TwoFactorDisable() {
  const queryClient = useQueryClient();
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: (pw: string) => disableTwoFactor(pw),
    onSuccess: (data) => {
      if (data.success) {
        toast.success("Two-factor disabled");
        setPassword("");
        void queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
      } else {
        toast.error("Disable failed", {
          description: "Password verification failed.",
        });
      }
    },
    onError: (err) =>
      toast.error("Disable failed", {
        description: apiErrorMessage(err, "Could not disable two-factor."),
      }),
  });

  return (
    <div className="space-y-3">
      <p className="text-sm text-[var(--color-muted-foreground)]">
        Two-factor is currently enabled. Confirm your password to disable —
        this rotates the authenticator secret, so a fresh enroll will generate
        a new QR code.
      </p>
      <div className="grid gap-3 sm:grid-cols-[1fr_auto] sm:items-end">
        <div className="space-y-1.5">
          <Label htmlFor="disable-pw">Current password</Label>
          <Input
            id="disable-pw"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="font-mono"
          />
        </div>
        <Button
          type="button"
          variant="destructive"
          onClick={() => mutation.mutate(password)}
          disabled={password.length === 0 || mutation.isPending}
        >
          <ShieldOff className="mr-1 h-3.5 w-3.5" />
          {mutation.isPending ? "Disabling…" : "Disable two-factor"}
        </Button>
      </div>
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
