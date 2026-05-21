import {
  useState,
  type FormEvent,
} from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import {
  AlertCircle,
  ArrowRight,
  Building2,
  Eye,
  EyeOff,
  FlaskConical,
  Loader2,
  Mail,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { AuthHeadline, AuthShell } from "@/components/auth/auth-shell";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";
import { LoginDemoPanel } from "@/pages/login.demo-panel";
import type { DemoAccount } from "@/pages/login.demo-accounts";

type LocationState = { from?: { pathname: string } };

// ────────────────────────────────────────────────────────────────────────
// Demo popup — DEV only. Click "Demo accounts" under the form, the
// LoginDemoPanel renders inside a Dialog. Picking an account closes the
// dialog and prefills the form.
// ────────────────────────────────────────────────────────────────────────

function DemoDialog({
  open,
  onOpenChange,
  current,
  onSelect,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
  current: { email: string; tenant: string };
  onSelect: (account: DemoAccount) => void;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-[520px] !p-0">
        <DialogTitle className="sr-only">Demo accounts</DialogTitle>
        <DialogDescription className="sr-only">
          Pick a seeded demo account to prefill the login form. DEV only.
        </DialogDescription>
        <div className="rounded-[inherit] overflow-hidden">
          <LoginDemoPanel
            current={current}
            onSelect={(a) => {
              onSelect(a);
              onOpenChange(false);
            }}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Page — renders inside AuthShell so the chrome matches forgot-password,
// reset-password, and confirm-email exactly. dentalOS warm-paper card on
// rose+saffron atmospheric orbs; tenant + email get leading icons to
// mirror the rest of the auth flow.
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

  const onPickDemo = (account: DemoAccount) => {
    setError(null);
    setEmail(account.email);
    setPassword(account.password);
    setTenant(account.tenant);
  };

  return (
    <>
      <AuthShell>
        <div className="mb-6 sm:mb-8">
          <AuthHeadline lead="Welcome" accent="back" />
          <p className="text-[13px] text-[var(--color-muted-foreground)]">
            Sign in to continue to your tenant.
          </p>
        </div>

        <form onSubmit={onSubmit} className="space-y-5" noValidate aria-describedby={error ? "login-error" : undefined}>
          <div className="space-y-1.5">
            <Label
              htmlFor="tenant"
              className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
            >
              Tenant
            </Label>
            <div className="relative">
              <Building2 className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]" />
              <Input
                id="tenant"
                value={tenant}
                onChange={(e) => setTenant(e.target.value)}
                placeholder="root"
                autoComplete="organization"
                required
                aria-invalid={error ? true : undefined}
                aria-describedby={error ? "login-error" : undefined}
                className="h-11 pl-10 text-[14px]"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label
              htmlFor="email"
              className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
            >
              Email
            </Label>
            <div className="relative">
              <Mail className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]" />
              <Input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                autoComplete="email"
                required
                aria-invalid={error ? true : undefined}
                aria-describedby={error ? "login-error" : undefined}
                className="h-11 pl-10 text-[14px]"
              />
            </div>
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
                aria-describedby={error ? "login-error" : undefined}
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

        {/* Demo accounts (DEV only) */}
        {import.meta.env.DEV && (
          <>
            <div className="mt-8 mb-5 flex items-center gap-3">
              <div className="h-px flex-1 bg-[var(--color-border)]" />
              <span className="text-[10px] font-semibold uppercase tracking-[0.15em] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
                Quick access
              </span>
              <div className="h-px flex-1 bg-[var(--color-border)]" />
            </div>

            <button
              type="button"
              onClick={() => setDemoOpen(true)}
              className={cn(
                "group flex w-full cursor-pointer items-center justify-between gap-2 rounded-lg border border-transparent px-3 py-2.5 text-left text-[12px]",
                "transition-all duration-150 hover:border-[var(--color-border)] hover:bg-[var(--color-secondary)]",
              )}
            >
              <span className="inline-flex items-center gap-2 text-[var(--color-muted-foreground)] group-hover:text-[var(--color-foreground)]">
                <FlaskConical className="size-3.5" />
                <span className="font-semibold uppercase tracking-wider text-[10.5px]">
                  Demo accounts
                </span>
              </span>
              <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[oklch(from_var(--color-saffron)_l_c_h_/_0.85)]">
                DEV
              </span>
            </button>
          </>
        )}
      </AuthShell>

      {/* DEV demo popup */}
      {import.meta.env.DEV && (
        <DemoDialog
          open={demoOpen}
          onOpenChange={setDemoOpen}
          current={{ email, tenant }}
          onSelect={onPickDemo}
        />
      )}
    </>
  );
}
