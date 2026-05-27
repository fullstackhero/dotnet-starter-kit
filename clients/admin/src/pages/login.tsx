import { useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import {
  AlertCircle,
  ArrowRight,
  ClipboardCheck,
  Copy,
  Eye,
  EyeOff,
  FlaskConical,
  Loader2,
  ShieldCheck,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { BrandMarkXL } from "@/components/brand-mark";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";

type LocationState = { from?: { pathname: string } };

const DEMO_PASSWORD = "Password123!";
const DEMO_SUPERADMIN = {
  tenant: "root",
  email: "superadmin@root.com",
  label: "SuperAdmin",
  persona: "Platform operator · cross-tenant control",
} as const;

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
  const [copied, setCopied] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login({ email, password, tenant });
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

  const onPickDemo = () => {
    setError(null);
    setEmail(DEMO_SUPERADMIN.email);
    setPassword(DEMO_PASSWORD);
    setTenant(DEMO_SUPERADMIN.tenant);
  };

  const onCopyPassword = async () => {
    try {
      await navigator.clipboard.writeText(DEMO_PASSWORD);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1400);
    } catch {
      // ignore
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-background)] px-5 py-8 sm:py-12">
      {/* Atmospheric background orbs */}
      <div className="pointer-events-none absolute inset-0" aria-hidden>
        <div
          className="absolute -top-[25%] -left-[15%] h-[70vw] w-[70vw] rounded-full blur-[140px]"
          style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.05)" }}
        />
        <div
          className="absolute -bottom-[20%] -right-[10%] h-[55vw] w-[55vw] rounded-full blur-[120px]"
          style={{ backgroundColor: "oklch(from var(--color-saffron, var(--color-primary)) l c h / 0.07)" }}
        />
        <div
          className="absolute top-[10%] right-[5%] h-[30vw] w-[30vw] rounded-full blur-[80px]"
          style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.025)" }}
        />
      </div>

      {/* Card column */}
      <div className="relative z-10 w-full max-w-[420px] fsh-enter fsh-enter-1">
        {/* Brand lockup */}
        <div className="mb-8 flex flex-col items-center">
          <BrandMarkXL />
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

            <form
              onSubmit={onSubmit}
              className="space-y-5"
              noValidate
              aria-describedby={error ? "login-error" : undefined}
            >
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

            {import.meta.env.DEV && (
              <div className="mt-7">
                <DevDemoCallout
                  active={email === DEMO_SUPERADMIN.email && tenant === DEMO_SUPERADMIN.tenant}
                  copied={copied}
                  onPick={onPickDemo}
                  onCopy={onCopyPassword}
                />
              </div>
            )}
          </div>
        </div>

        <div className="mt-6 flex items-center justify-center gap-1.5 text-[11px] text-[var(--color-muted-foreground)]">
          <ShieldCheck className="size-3" />
          <span>Encrypted in transit · JWT-secured session</span>
        </div>
        <p className="mt-4 text-center text-[10px] font-medium uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]">
          FullStackHero Administration
        </p>
      </div>
    </div>
  );
}

// ─── subcomponents ───────────────────────────────────────────────────

function DevDemoCallout({
  active,
  copied,
  onPick,
  onCopy,
}: {
  active: boolean;
  copied: boolean;
  onPick: () => void;
  onCopy: () => void;
}) {
  return (
    <div
      role="region"
      aria-label="Development demo account"
      className="relative overflow-hidden rounded-lg border border-dashed border-[oklch(from_var(--color-warning)_l_c_h_/_0.35)] bg-[oklch(from_var(--color-warning)_l_c_h_/_0.04)] p-3.5"
    >
      <div className="flex items-start gap-2.5">
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[oklch(from_var(--color-warning)_l_c_h_/_0.14)] text-[var(--color-warning)] ring-1 ring-inset ring-[oklch(from_var(--color-warning)_l_c_h_/_0.35)]"
        >
          <FlaskConical className="h-3.5 w-3.5" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-warning)]">
            Dev · demo account
          </div>
          <button
            type="button"
            onClick={onPick}
            aria-pressed={active}
            className={cn(
              "mt-2 flex w-full items-center gap-2 rounded-lg border px-2.5 py-1.5 text-left transition-colors",
              active
                ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)]"
                : "border-[var(--color-border)] bg-transparent hover:bg-[var(--color-accent)]",
            )}
          >
            <span className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-[var(--color-primary)] text-[10px] font-bold text-[var(--color-primary-foreground)]">
              SA
            </span>
            <span className="min-w-0 flex-1">
              <span className="flex items-center gap-1.5">
                <span className="text-[12.5px] font-semibold tracking-tight text-[var(--color-foreground)]">
                  {DEMO_SUPERADMIN.label}
                </span>
                <span className="rounded bg-[var(--color-muted)] px-1 py-0.5 font-mono text-[10px] text-[var(--color-muted-foreground)]">
                  root
                </span>
              </span>
              <span className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                {DEMO_SUPERADMIN.email}
              </span>
              <span className="mt-0.5 block text-[11px] italic text-[var(--color-muted-foreground)]/80">
                {DEMO_SUPERADMIN.persona}
              </span>
            </span>
            <span
              className={cn(
                "shrink-0 text-[10px] font-semibold uppercase tracking-wider",
                active ? "text-[var(--color-primary)]" : "text-[var(--color-muted-foreground)]",
              )}
            >
              {active ? "loaded" : "use →"}
            </span>
          </button>

          <div className="mt-3 flex items-center justify-between text-[11px]">
            <span className="flex items-center gap-1.5 text-[var(--color-muted-foreground)]">
              <span className="text-[10.5px] uppercase tracking-wider">password</span>
              <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] text-[var(--color-foreground)]">
                {DEMO_PASSWORD}
              </code>
            </span>
            <button
              type="button"
              onClick={onCopy}
              className={cn(
                "inline-flex h-6 cursor-pointer items-center gap-1 rounded-md px-2 text-[10px] font-semibold uppercase tracking-wider transition-colors",
                copied
                  ? "bg-[oklch(from_var(--color-success)_l_c_h_/_0.12)] text-[var(--color-success)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)]",
              )}
            >
              {copied ? (
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
      </div>
    </div>
  );
}
