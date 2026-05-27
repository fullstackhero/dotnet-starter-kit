import { useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, Check, MailCheck } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { AuthShell } from "@/components/auth/auth-shell";
import { requestPasswordReset } from "@/api/users";
import { ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";

/**
 * Forgot-password — admin variant.
 *
 * Same security contract as the dashboard variant: server returns 200
 * for any input, so the UI must NOT branch on response shape to imply
 * account existence. Always render the same "check your inbox" success.
 */
export function ForgotPasswordPage() {
  const { isAuthenticated } = useAuth();
  const [email, setEmail] = useState("");
  const [tenant, setTenant] = useState(env.defaultTenant);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () => requestPasswordReset({ email, tenant }),
    onSuccess: () => setSubmitted(true),
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      setError(detail);
    },
  });

  if (isAuthenticated) return <Navigate to="/" replace />;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    mutation.mutate();
  };

  return (
    <AuthShell
      crumbLeft="// RECOVER ACCOUNT"
      crumbRight="issue reset token"
      blurb={
        submitted
          ? "We routed a reset link to that mailbox. Hit your inbox."
          : "Enter the email + tenant you sign in with. We'll dispatch a one-time reset link."
      }
    >
      {submitted ? (
        <div className="fsh-enter space-y-5">
          <div className="grid place-items-center">
            <span
              aria-hidden
              className="grid h-12 w-12 place-items-center rounded-full border border-[oklch(from_var(--color-success)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]"
            >
              <MailCheck className="h-5 w-5" />
            </span>
          </div>
          <div className="space-y-2 text-center">
            <h2 className="text-base font-semibold tracking-tight">Check your inbox.</h2>
            <p className="text-sm leading-relaxed text-[var(--color-muted-foreground)]">
              If an account exists for{" "}
              <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[12px] text-[var(--color-foreground)]">
                {email}
              </code>{" "}
              in tenant{" "}
              <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[12px] text-[var(--color-foreground)]">
                {tenant}
              </code>
              , a one-time reset link is on its way. The link expires in 30 minutes.
            </p>
          </div>
          <ul className="space-y-1.5 text-[12.5px] text-[var(--color-muted-foreground)]">
            <li className="flex items-start gap-2">
              <Check className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[var(--color-success)]" />
              Didn't get it? Wait a minute, then check spam.
            </li>
            <li className="flex items-start gap-2">
              <Check className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[var(--color-success)]" />
              Still nothing? Confirm the email + tenant and retry.
            </li>
          </ul>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setSubmitted(false);
                setError(null);
              }}
            >
              Try a different address
            </Button>
            <Link to="/login" className="ml-auto">
              <Button type="button" variant="ghost">
                Back to sign in
              </Button>
            </Link>
          </div>
        </div>
      ) : (
        <form onSubmit={onSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="reset-tenant">Tenant</Label>
            <Input
              id="reset-tenant"
              value={tenant}
              onChange={(e) => setTenant(e.target.value)}
              required
              autoComplete="organization"
              placeholder="root"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="reset-email">Email</Label>
            <Input
              id="reset-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              autoFocus
              placeholder="operator@root.example"
            />
          </div>

          {error && (
            <div
              role="alert"
              className="flex items-start gap-2 rounded-md border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-sm text-[var(--color-destructive)]"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <span className="leading-snug">{error}</span>
            </div>
          )}

          <Button
            type="submit"
            variant="signal"
            className="w-full"
            disabled={mutation.isPending || !email || !tenant}
          >
            {mutation.isPending ? "Dispatching link…" : "Send reset link →"}
          </Button>

          <div className="text-center text-xs text-[var(--color-muted-foreground)]">
            Remembered it?{" "}
            <Link
              to="/login"
              className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
            >
              Sign in
            </Link>
          </div>
        </form>
      )}
    </AuthShell>
  );
}
