import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, Navigate, useNavigate, useSearchParams } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, ArrowRight, Check, Loader2, ShieldCheck } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { AuthHeadline, AuthShell, FloatField } from "@/components/auth/auth-shell";
import { resetPassword } from "@/api/identity";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * Reset-password — step 2 of the reset flow. The user lands here from a
 * link emailed by the forgot-password endpoint:
 *
 *   /reset-password?token=<urlencoded>&email=<urlencoded>&tenant=<id>
 *
 * Token + email + tenant come from URL; password comes from the form.
 * On success we toast and redirect to /login (existing JWTs stay valid
 * until natural expiry — a fresh sign-in under the new password is the
 * cleanest UX).
 */

type Strength = "weak" | "fair" | "strong";

function scorePassword(value: string): Strength | null {
  if (value.length === 0) return null;
  if (value.length < 8) return "weak";

  let score = 0;
  if (/[a-z]/.test(value)) score++;
  if (/[A-Z]/.test(value)) score++;
  if (/\d/.test(value)) score++;
  if (/[^A-Za-z0-9]/.test(value)) score++;
  if (value.length >= 12) score++;

  if (score <= 2) return "weak";
  if (score === 3) return "fair";
  return "strong";
}

const STRENGTH_META: Record<Strength, { label: string; fill: string; bar: string }> = {
  weak: {
    label: "Weak",
    fill: "bg-[var(--color-destructive)]",
    bar: "w-1/3",
  },
  fair: {
    label: "Fair",
    fill: "bg-[var(--color-warning)]",
    bar: "w-2/3",
  },
  strong: {
    label: "Strong",
    fill: "bg-[var(--color-success)]",
    bar: "w-full",
  },
};

export function ResetPasswordPage() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();

  // All three are required for the server call. We treat the page as
  // "malformed link" if any are missing rather than letting the user
  // submit a doomed request.
  const token = params.get("token") ?? "";
  const email = params.get("email") ?? "";
  const tenant = params.get("tenant") ?? "";

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);

  const strength = useMemo(() => scorePassword(password), [password]);
  const matches = password.length > 0 && password === confirm;

  const mutation = useMutation({
    mutationFn: () => resetPassword({ email, password, token, tenant }),
    onSuccess: () => {
      toast.success("Password updated", {
        description: "Sign in with your new password to continue.",
      });
      navigate("/login", { replace: true });
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      setError(detail);
    },
  });

  // Keep the error in sync with the form: typing after an error should
  // clear it rather than persist a stale message under the new input.
  useEffect(() => {
    setError(null);
  }, [password, confirm]);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const malformed = !token || !email || !tenant;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!matches) {
      setError("Passwords don't match.");
      return;
    }
    if (password.length < 8) {
      setError("Use at least 8 characters.");
      return;
    }
    mutation.mutate();
  };

  return (
    <AuthShell
      eyebrow="// 03.RESET-PASSWORD"
      tagline="token · scoped"
      footer={
        <span>
          Changed your mind?{" "}
          <Link
            to="/login"
            className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
          >
            Sign in
          </Link>
        </span>
      }
    >
      {malformed ? (
        <div className="space-y-3">
          <AuthHeadline lead="This link is" accent="incomplete." />
          <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
            The reset link is missing one of <code className="rounded bg-[var(--color-muted)] px-1 font-mono text-[11.5px]">token</code>,{" "}
            <code className="rounded bg-[var(--color-muted)] px-1 font-mono text-[11.5px]">email</code>, or{" "}
            <code className="rounded bg-[var(--color-muted)] px-1 font-mono text-[11.5px]">tenant</code>.
            Some email clients clip long URLs — try copy-pasting the full link from the
            original email into your browser's address bar.
          </p>
          <div className="flex gap-2 pt-2">
            <Link to="/forgot-password">
              <Button type="button" variant="outline">
                Request a new link
              </Button>
            </Link>
            <Link to="/login">
              <Button type="button" variant="ghost">
                Back to sign in
              </Button>
            </Link>
          </div>
        </div>
      ) : (
        <>
          <AuthHeadline lead="Set a new" accent="password." />
          <p className="mt-1 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
            Resetting password for{" "}
            <span className="text-[var(--color-foreground)]">{email}</span> on{" "}
            <code className="rounded bg-[var(--color-muted)] px-1 font-mono text-[11.5px] text-[var(--color-foreground)]">
              {tenant}
            </code>
            .
          </p>

          <form onSubmit={onSubmit} className="mt-5 space-y-3" noValidate>
            <FloatField
              id="new-password"
              label="New password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="new-password"
              autoFocus
              minLength={8}
            />

            {strength && (
              <div className="fsh-enter -mt-1 flex items-center gap-2 pl-1">
                <div className="h-1 flex-1 overflow-hidden rounded-full bg-[var(--color-border)]/60">
                  <div
                    className={cn(
                      "h-full transition-all duration-[var(--duration-default)]",
                      STRENGTH_META[strength].fill,
                      STRENGTH_META[strength].bar,
                    )}
                  />
                </div>
                <span className="min-w-[3.5rem] text-right font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                  {STRENGTH_META[strength].label}
                </span>
              </div>
            )}

            <FloatField
              id="confirm-password"
              label="Confirm password"
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              required
              autoComplete="new-password"
              minLength={8}
            />

            {confirm.length > 0 && (
              <div
                className={cn(
                  "flex items-center gap-1.5 pl-1 text-[11.5px]",
                  matches
                    ? "text-[var(--color-success)]"
                    : "text-[var(--color-muted-foreground)]",
                )}
              >
                <Check
                  className={cn(
                    "h-3.5 w-3.5",
                    matches ? "opacity-100" : "opacity-40",
                  )}
                />
                <span>{matches ? "Passwords match" : "Doesn't match yet"}</span>
              </div>
            )}

            {error && (
              <div
                role="alert"
                className="fsh-enter flex items-start gap-2 rounded-md border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.40)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-sm text-[var(--color-destructive)]"
              >
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                <span className="leading-snug">{error}</span>
              </div>
            )}

            <Button
              type="submit"
              size="lg"
              className="btn-shimmer mt-1.5 w-full"
              disabled={mutation.isPending || !matches || password.length < 8}
            >
              {mutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Updating password…
                </>
              ) : (
                <>
                  <ShieldCheck className="h-4 w-4" />
                  Set new password
                  <ArrowRight className="h-4 w-4 transition-transform duration-[var(--duration-default)] group-hover/btn:translate-x-0.5" />
                </>
              )}
            </Button>
          </form>
        </>
      )}
    </AuthShell>
  );
}
