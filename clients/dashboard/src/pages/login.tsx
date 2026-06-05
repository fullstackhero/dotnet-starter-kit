import { useEffect, useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { AlertCircle, ArrowRight, Eye, EyeOff, Loader2, Sparkles, TimerOff } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { consumeSignedOutReason } from "@/auth/inactivity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { AuthShell } from "@/components/auth/auth-shell";
import { DemoAccountsDialog } from "@/components/auth/demo-accounts-dialog";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";
import type { DemoAccount } from "@/pages/login.demo-accounts";

type LocationState = { from?: { pathname: string } };

// ────────────────────────────────────────────────────────────────────────
// Login — dentalOS "welcome back" card on rose+saffron atmospheric orbs
// (chrome supplied by AuthShell, shared with the rest of the auth flow).
// FSH stays multi-tenant, so the Tenant field leads the form; Email +
// Password follow. The demo picker ("Step into any role") signs in
// instantly and is gated on the runtime demoMode flag — on in staging,
// off in production.
// ────────────────────────────────────────────────────────────────────────

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as LocationState | null)?.from?.pathname ?? "/";

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [tenant, setTenant] = useState(env.defaultTenant);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [demoOpen, setDemoOpen] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  // Surface why the previous session ended (read-and-clear, one-shot).
  useEffect(() => {
    if (consumeSignedOutReason() === "inactivity") {
      setNotice("You were signed out due to inactivity.");
    }
  }, []);

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  const performLogin = async (creds: { email: string; password: string; tenant: string }) => {
    setError(null);
    setSubmitting(true);
    try {
      await login(creds);
      navigate(from, { replace: true });
    } catch (err) {
      const message =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : err instanceof Error
            ? err.message
            : "Login failed";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  };

  const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    await performLogin({ email, password, tenant });
  };

  // Demo picker → reflect the chosen creds in the form, then sign in
  // instantly. Each demo account carries its own tenant + password.
  const onPickDemo = (account: DemoAccount) => {
    setEmail(account.email);
    setPassword(account.password);
    setTenant(account.tenant);
    void performLogin({ email: account.email, password: account.password, tenant: account.tenant });
  };

  return (
    <>
      <AuthShell>
        <div className="mb-6 sm:mb-8">
          <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
            Welcome back
          </h1>
          <p className="text-[13px] text-[var(--color-muted-foreground)]">
            Sign in to your account
          </p>
        </div>

        {notice && (
          <div
            role="status"
            className="mb-5 flex items-start gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2.5 text-[12.5px] leading-snug text-[var(--color-muted-foreground)] fsh-enter"
          >
            <TimerOff className="mt-0.5 size-3.5 shrink-0 text-[var(--color-muted-foreground)]" />
            <span>{notice}</span>
          </div>
        )}

        <form
          onSubmit={onSubmit}
          className="space-y-5"
          noValidate
          aria-describedby={error ? "login-error" : undefined}
        >
          {/* Tenant — FSH stays multi-tenant, so this leads the form. */}
          <div className="space-y-1.5">
            <Label
              htmlFor="tenant"
              className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
            >
              Tenant
            </Label>
            <Input
              id="tenant"
              value={tenant}
              onChange={(e) => setTenant(e.target.value)}
              placeholder="root"
              autoComplete="organization"
              required
              aria-invalid={error ? true : undefined}
              className="h-11 text-[14px]"
            />
          </div>

          <div className="space-y-1.5">
            <Label
              htmlFor="email"
              className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
            >
              Email
            </Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@example.com"
              autoComplete="email"
              required
              aria-invalid={error ? true : undefined}
              className="h-11 text-[14px]"
            />
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <Label
                htmlFor="password"
                className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
              >
                Password
              </Label>
              <Link
                to="/forgot-password"
                className="text-[11px] font-medium text-[var(--color-muted-foreground)] underline-offset-4 transition-colors hover:text-[var(--color-primary)] hover:underline"
              >
                Forgot?
              </Link>
            </div>
            <div className="relative">
              <Input
                id="password"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                autoComplete="current-password"
                required
                aria-invalid={error ? true : undefined}
                className="h-11 pr-11 text-[14px]"
              />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                aria-label={showPassword ? "Hide password" : "Show password"}
                className="absolute right-3.5 top-1/2 grid h-6 w-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
              >
                {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
              </button>
            </div>
          </div>

          {error && (
            <div
              id="login-error"
              role="alert"
              className={cn(
                "fsh-enter flex items-start gap-2 rounded-lg border px-3 py-2 text-sm",
                "border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)]",
                "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)]",
                "text-[var(--color-destructive)]",
              )}
            >
              <AlertCircle className="mt-0.5 size-4 shrink-0" />
              <span className="leading-snug">{error}</span>
            </div>
          )}

          <div className="pt-1.5">
            <Button
              type="submit"
              disabled={submitting || !email || !password || !tenant}
              className="group h-11 w-full text-[14px] font-semibold"
            >
              {submitting ? (
                <>
                  <Loader2 className="size-4 animate-spin" />
                  <span>Signing in…</span>
                </>
              ) : (
                <>
                  <span>Sign in</span>
                  <ArrowRight className="size-[14px] opacity-60 transition-all duration-200 group-hover:translate-x-0.5 group-hover:opacity-100" />
                </>
              )}
            </Button>
          </div>
        </form>

        {/* Demo accounts — runtime-gated (staging on, prod off). */}
        {env.demoMode && (
          <div className="mt-7">
            <button
              type="button"
              onClick={() => setDemoOpen(true)}
              className="flex h-10 w-full cursor-pointer items-center justify-center gap-2 rounded-lg border border-dashed border-primary/25 bg-transparent text-[12.5px] font-medium text-primary/70 transition-all duration-150 hover:border-primary/40 hover:bg-primary/[0.04] hover:text-primary"
            >
              <Sparkles className="size-[13px]" />
              <span>Sign in with a demo account</span>
            </button>
          </div>
        )}
      </AuthShell>

      {env.demoMode && (
        <DemoAccountsDialog open={demoOpen} onOpenChange={setDemoOpen} onPick={onPickDemo} />
      )}
    </>
  );
}
