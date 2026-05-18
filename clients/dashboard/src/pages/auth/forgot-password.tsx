import { useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, ArrowRight, Check, Loader2, MailCheck } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { AuthHeadline, AuthShell, FloatField } from "@/components/auth/auth-shell";
import { requestPasswordReset } from "@/api/identity";
import { ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";

/**
 * Forgot-password — step 1 of the reset flow. Collects (email, tenant)
 * and asks the server to email a one-time reset link to that address.
 *
 * Security note: the backend deliberately returns 200 even when the email
 * doesn't exist (no account-enumeration). The UI must NOT branch on the
 * server response to imply existence — always render the same "check your
 * inbox" success state after a 2xx.
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
      // Most failures here are infra (tenant not resolvable, server down) —
      // we surface those plainly; account-existence is intentionally hidden
      // by the server's uniform 200 response.
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      setError(detail);
    },
  });

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    mutation.mutate();
  };

  return (
    <AuthShell
      eyebrow="// 02.FORGOT-PASSWORD"
      tagline="email · token"
      footer={
        <span>
          Remembered it?{" "}
          <Link
            to="/login"
            className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
          >
            Sign in
          </Link>
        </span>
      }
    >
      {submitted ? (
        <div className="fsh-enter space-y-4">
          <div className="grid place-items-center pb-1 pt-2">
            <span
              aria-hidden
              className="grid h-12 w-12 place-items-center rounded-full border border-[oklch(from_var(--color-success)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]"
            >
              <MailCheck className="h-5 w-5" />
            </span>
          </div>
          <AuthHeadline lead="Check your" accent="inbox." />
          <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
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
          <ul className="space-y-1.5 text-[12.5px] text-[var(--color-muted-foreground)]">
            <li className="flex items-start gap-2">
              <Check className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[var(--color-success)]" />
              Didn't get it? Wait a minute, then check spam.
            </li>
            <li className="flex items-start gap-2">
              <Check className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[var(--color-success)]" />
              Still nothing? Confirm the email + tenant and try again.
            </li>
          </ul>
          <div className="flex items-center gap-2 pt-2">
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                setSubmitted(false);
                setError(null);
              }}
            >
              Try a different address
            </Button>
            <Link to="/login" className="ml-auto">
              <Button type="button" variant="outline">
                Back to sign in
              </Button>
            </Link>
          </div>
        </div>
      ) : (
        <>
          <AuthHeadline lead="Reset your" accent="password." />
          <p className="mt-1 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
            Enter the email you sign in with. We'll send a one-time link.
          </p>

          <form onSubmit={onSubmit} className="mt-5 space-y-3" noValidate>
            <FloatField
              id="reset-tenant"
              label="Tenant"
              value={tenant}
              onChange={(e) => setTenant(e.target.value)}
              required
              autoComplete="organization"
            />
            <FloatField
              id="reset-email"
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              autoFocus
            />

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
              disabled={mutation.isPending || !email || !tenant}
            >
              {mutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Sending link…
                </>
              ) : (
                <>
                  Send reset link
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
