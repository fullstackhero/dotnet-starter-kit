import { useEffect, useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import {
  AlertCircle,
  ArrowRight,
  Eye,
  EyeOff,
  Loader2,
  ShieldCheck,
  Sparkles,
  TimerOff,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { consumeSignedOutReason } from "@/auth/inactivity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DemoAccountsDialog } from "@/components/auth/demo-accounts-dialog";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";
import type { DemoAccount } from "@/pages/login.demo-accounts";

// ────────────────────────────────────────────────────────────────────────
// Login — FSH Admin operator sign-in.
// Chrome mirrors the dashboard's AuthShell: atmospheric orbs, FSH logo
// lockup, warm-paper card with backdrop blur.
// The dev demo button ("Sign in with a demo account") opens the same
// popup dialog UX the dashboard uses — pick an account → fills
// tenant/email/password → instant sign-in. Gated on import.meta.env.DEV
// (admin has no runtime demoMode flag).
// ────────────────────────────────────────────────────────────────────────

type LocationState = { from?: { pathname: string } };

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

  // Demo picker → reflect the chosen creds in the form, then sign in instantly.
  const onPickDemo = (account: DemoAccount) => {
    setEmail(account.email);
    setPassword(account.password);
    setTenant(account.tenant);
    void performLogin({ email: account.email, password: account.password, tenant: account.tenant });
  };

  return (
    <>
      {/* ── Outer shell — matches dashboard AuthShell exactly ─────────── */}
      <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-background)] px-5 py-8 sm:py-12">
        {/* Atmospheric background orbs */}
        <div className="pointer-events-none absolute inset-0" aria-hidden>
          <div
            className="absolute -top-[25%] -left-[15%] h-[70vw] w-[70vw] rounded-full blur-[140px]"
            style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.05)" }}
          />
          <div
            className="absolute -bottom-[20%] -right-[10%] h-[55vw] w-[55vw] rounded-full blur-[120px]"
            style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.07)" }}
          />
          <div
            className="absolute top-[10%] right-[5%] h-[30vw] w-[30vw] rounded-full blur-[80px]"
            style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.025)" }}
          />
        </div>

        {/* Card column */}
        <div className="relative z-10 w-full max-w-[420px] fsh-enter fsh-enter-1">
          {/* ── Brand lockup — same as dashboard AuthShell ─────────────── */}
          <div className="mb-8 flex flex-col items-center">
            <div className="flex items-center gap-2.5">
              <img
                src="/logo-fullstackhero.png"
                alt="fullstackhero"
                className="size-9 object-contain"
              />
              <span className="font-display text-[26px] font-semibold tracking-tight text-[var(--color-foreground)]">
                fullstack<span className="text-[var(--color-primary)]">hero</span>
              </span>
            </div>
            <div className="mt-3 flex items-center gap-2 text-[10px] font-semibold uppercase tracking-[0.2em] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
              <span aria-hidden className="h-px w-6 bg-[var(--color-border)]" />
              <span>Platform Admin</span>
              <span aria-hidden className="h-px w-6 bg-[var(--color-border)]" />
            </div>
          </div>

          {/* Form card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[oklch(from_var(--color-card)_l_c_h_/_0.85)] shadow-[0_1px_3px_oklch(0_0_0_/_0.04),0_8px_24px_oklch(0_0_0_/_0.06)] backdrop-blur-xl">
            <div className="px-6 py-7 sm:px-8 sm:py-9">
              <div className="mb-6 sm:mb-8">
                <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
                  Welcome back
                </h1>
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  Sign in to your operator account
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
                {/* Tenant */}
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
                    autoComplete="organization"
                    placeholder="root"
                    required
                    aria-invalid={error ? true : undefined}
                    className="h-11 text-[14px]"
                  />
                </div>

                {/* Email */}
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
                    autoComplete="email"
                    placeholder="operator@root.example"
                    required
                    aria-invalid={error ? true : undefined}
                    className="h-11 text-[14px]"
                  />
                </div>

                {/* Password */}
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
                      autoComplete="current-password"
                      placeholder="Enter your password"
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

              {/* Demo accounts — dev-only, same dashed-button pattern as dashboard. */}
              {import.meta.env.DEV && (
                <div className="mt-7">
                  <button
                    type="button"
                    onClick={() => setDemoOpen(true)}
                    className="flex h-10 w-full cursor-pointer items-center justify-center gap-2 rounded-lg border border-dashed border-[var(--color-primary)]/25 bg-transparent text-[12.5px] font-medium text-[var(--color-primary)]/70 transition-all duration-150 hover:border-[var(--color-primary)]/40 hover:bg-[var(--color-primary)]/[0.04] hover:text-[var(--color-primary)]"
                  >
                    <Sparkles className="size-[13px]" />
                    <span>Sign in with a demo account</span>
                  </button>
                </div>
              )}
            </div>
          </div>

          <div className="mt-6 flex items-center justify-center gap-1.5 text-[11px] text-[var(--color-muted-foreground)]">
            <ShieldCheck className="size-3" />
            <span>Encrypted in transit · JWT-secured session</span>
          </div>
          <p className="mt-4 text-center text-[10px] font-medium uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]">
            fullstackhero Administration
          </p>
        </div>
      </div>

      {/* Demo dialog — rendered outside the shell to escape any overflow clipping. */}
      {import.meta.env.DEV && (
        <DemoAccountsDialog open={demoOpen} onOpenChange={setDemoOpen} onPick={onPickDemo} />
      )}
    </>
  );
}
