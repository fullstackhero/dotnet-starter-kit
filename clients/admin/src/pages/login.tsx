import { useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { ClipboardCheck, Copy, FlaskConical } from "lucide-react";
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

/**
 * LoginPage — editorial split-screen. Left pane (hidden below lg) is the
 * brand stage: 32px coordinate-grid mesh + ASCII-style monogram + a soft
 * chartreuse vignette anchored top-left. Right pane is the form, which
 * gets the same hairline-everything treatment as the rest of Console.
 */
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
    <div className="grid min-h-screen bg-[var(--color-background)] text-[var(--color-foreground)] lg:grid-cols-[1.1fr_1fr]">
      {/* ─── Left pane — brand stage ───────────────────────────────── */}
      <aside className="relative hidden overflow-hidden border-r border-[var(--color-border)] lg:flex lg:flex-col">
        {/* Coordinate-grid mesh */}
        <div className="canvas-mesh pointer-events-none absolute inset-0" aria-hidden />

        {/* Top-left chartreuse vignette */}
        <div
          className="pointer-events-none absolute inset-0"
          aria-hidden
          style={{
            background:
              "radial-gradient(48rem 32rem at 0% 0%, oklch(from var(--color-accent-signal) l c h / 0.18), transparent 65%)",
          }}
        />

        {/* Corner ticks — magazine-style coordinate marks */}
        <CornerTicks />

        <div className="relative flex flex-1 flex-col justify-between p-12 xl:p-16">
          {/* Top crumb */}
          <div className="meta text-[var(--color-muted-foreground)] fsh-enter">
            // FSH / CONSOLE / SIGN IN
          </div>

          {/* Hero monogram */}
          <BrandMarkXL className="fsh-enter fsh-enter-2 max-w-lg" />

          {/* Bottom meta */}
          <div className="fsh-enter fsh-enter-4 flex items-end justify-between gap-6">
            <div className="space-y-1">
              <div className="meta text-[var(--color-muted-foreground)]">authorized personnel</div>
              <div className="font-mono text-[12px] text-[var(--color-muted-foreground)] leading-relaxed">
                Tenant administrators sign in through their tenant dashboard.
                <br />
                This surface is for the root tenant operator.
              </div>
            </div>
            <div className="meta text-right text-[var(--color-muted-foreground)]">
              v0.1
              <br />
              build · live
            </div>
          </div>
        </div>
      </aside>

      {/* ─── Right pane — form ─────────────────────────────────────── */}
      <main className="relative flex flex-col items-center justify-center p-6 lg:p-10">
        {/* Subtle subgrid on the form pane too */}
        <div className="canvas-grid pointer-events-none absolute inset-0" aria-hidden />

        <div className="relative w-full max-w-md space-y-6 fsh-enter">
          {/* Mobile-only brand (lg+ uses the left pane) */}
          <div className="lg:hidden">
            <BrandMarkXL />
          </div>

          {/* Section rule + form header */}
          <div className="space-y-3">
            <div className="section-rule">
              <span className="section-rule__crumb">// AUTHENTICATE</span>
              <span className="section-rule__crumb section-rule__crumb--muted">
                request a session
              </span>
            </div>
            <p className="text-sm text-[var(--color-muted-foreground)]">
              Use a root-tenant operator account. Tenant administrators sign in through their own tenant.
            </p>
          </div>

          <form onSubmit={onSubmit} className="space-y-4">
            <Field
              id="tenant"
              label="Tenant"
              value={tenant}
              onChange={setTenant}
              autoComplete="organization"
              placeholder="root"
              required
            />
            <Field
              id="email"
              label="Email"
              type="email"
              value={email}
              onChange={setEmail}
              autoComplete="email"
              placeholder="operator@root.example"
              required
            />
            <Field
              id="password"
              label="Password"
              type="password"
              value={password}
              onChange={setPassword}
              autoComplete="current-password"
              required
            />

            {error && (
              <div className="rounded-md border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-sm text-[var(--color-destructive)]">
                {error}
              </div>
            )}

            <Button type="submit" variant="signal" className="w-full" disabled={submitting}>
              {submitting ? "Authenticating…" : "Sign in →"}
            </Button>

            <div className="flex justify-end">
              <Link
                to="/forgot-password"
                className="meta text-[var(--color-muted-foreground)] underline-offset-4 transition-colors hover:text-[var(--color-foreground)] hover:underline"
              >
                // forgot password?
              </Link>
            </div>
          </form>

          {import.meta.env.DEV && (
            <DevDemoCallout
              active={email === DEMO_SUPERADMIN.email && tenant === DEMO_SUPERADMIN.tenant}
              copied={copied}
              onPick={onPickDemo}
              onCopy={onCopyPassword}
            />
          )}
        </div>
      </main>
    </div>
  );
}

// ─── subcomponents ───────────────────────────────────────────────────

function CornerTicks() {
  // L-shaped tick marks at each corner, in the chartreuse signal.
  // Reads as "this surface has coordinates" without being a literal crosshair.
  const TICK = "h-3 w-3 border-[var(--color-accent-signal)]";
  return (
    <>
      <span className={cn("pointer-events-none absolute left-6 top-6 border-l-2 border-t-2", TICK)} aria-hidden />
      <span className={cn("pointer-events-none absolute right-6 top-6 border-r-2 border-t-2", TICK)} aria-hidden />
      <span className={cn("pointer-events-none absolute left-6 bottom-6 border-l-2 border-b-2", TICK)} aria-hidden />
      <span className={cn("pointer-events-none absolute right-6 bottom-6 border-r-2 border-b-2", TICK)} aria-hidden />
    </>
  );
}

type FieldProps = {
  id: string;
  label: string;
  value: string;
  onChange: (v: string) => void;
  type?: string;
  required?: boolean;
  placeholder?: string;
  autoComplete?: string;
};

function Field({
  id,
  label,
  value,
  onChange,
  type,
  required,
  placeholder,
  autoComplete,
}: FieldProps) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        type={type ?? "text"}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        placeholder={placeholder}
        autoComplete={autoComplete}
      />
    </div>
  );
}

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
      className="relative overflow-hidden rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-3.5"
    >
      <div className="flex items-start gap-2.5">
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[oklch(from_var(--color-warning)_l_c_h_/_0.16)] text-[var(--color-warning)] ring-1 ring-inset ring-[oklch(from_var(--color-warning)_l_c_h_/_0.4)]"
        >
          <FlaskConical className="h-3.5 w-3.5" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="meta flex items-center gap-2 text-[var(--color-warning)]">
            DEV · demo account
          </div>
          <button
            type="button"
            onClick={onPick}
            aria-pressed={active}
            className={cn(
              "mt-2 flex w-full items-center gap-2 rounded-md border px-2.5 py-1.5 text-left transition-colors",
              active
                ? "border-[var(--color-accent-signal)] bg-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.10)]"
                : "border-[var(--color-border)] bg-transparent hover:bg-[var(--color-muted)]",
            )}
          >
            <span className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-[var(--color-primary)] text-[10px] font-bold text-[var(--color-primary-foreground)]">
              SA
            </span>
            <span className="min-w-0 flex-1">
              <span className="flex items-center gap-1.5">
                <span className="text-[12.5px] font-semibold tracking-tight">
                  {DEMO_SUPERADMIN.label}
                </span>
                <span className="code-chip">root</span>
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
                "shrink-0 font-mono text-[10px] uppercase tracking-[0.14em]",
                active ? "text-[var(--color-accent-signal)]" : "text-[var(--color-muted-foreground)]",
              )}
            >
              {active ? "loaded" : "use →"}
            </span>
          </button>

          <div className="mt-3 flex items-center justify-between text-[11px]">
            <span className="flex items-center gap-1.5 text-[var(--color-muted-foreground)]">
              <span className="meta">password</span>
              <code className="code-chip">{DEMO_PASSWORD}</code>
            </span>
            <button
              type="button"
              onClick={onCopy}
              className={cn(
                "inline-flex h-6 cursor-pointer items-center gap-1 rounded-md px-2 font-mono text-[10px] uppercase tracking-[0.14em] transition-colors",
                copied
                  ? "bg-[oklch(from_var(--color-success)_l_c_h_/_0.16)] text-[var(--color-success)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]",
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
