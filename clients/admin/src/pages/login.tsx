import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ClipboardCheck, Copy, FlaskConical, ShieldAlert } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
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
    <div className="flex min-h-screen items-center justify-center bg-[var(--color-background)] p-6">
      <div className="w-full max-w-sm space-y-4">
        <Card>
          <CardHeader>
            <CardTitle>Sign in</CardTitle>
            <CardDescription>FullStackHero Admin · platform console</CardDescription>
          </CardHeader>
          <form onSubmit={onSubmit}>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="tenant">Tenant</Label>
                <Input
                  id="tenant"
                  value={tenant}
                  onChange={(e) => setTenant(e.target.value)}
                  required
                  autoComplete="organization"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="password">Password</Label>
                <Input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
                />
              </div>
              {error && (
                <div className="rounded-md border border-[var(--color-destructive)]/40 bg-[var(--color-destructive)]/10 px-3 py-2 text-sm text-[var(--color-destructive)]">
                  {error}
                </div>
              )}
            </CardContent>
            <CardFooter>
              <Button type="submit" className="w-full" disabled={submitting}>
                {submitting ? "Signing in…" : "Sign in"}
              </Button>
            </CardFooter>
          </form>
        </Card>

        {import.meta.env.DEV && (
          <DevDemoCallout
            active={email === DEMO_SUPERADMIN.email && tenant === DEMO_SUPERADMIN.tenant}
            copied={copied}
            onPick={onPickDemo}
            onCopy={onCopyPassword}
          />
        )}
      </div>
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
      className={cn(
        "relative overflow-hidden rounded-xl border px-4 py-3.5",
        "border-amber-500/40 bg-amber-500/5",
      )}
    >
      <div className="flex items-start gap-2.5">
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-amber-500/15 text-amber-600 ring-1 ring-inset ring-amber-500/40 dark:text-amber-400"
        >
          <FlaskConical className="h-3.5 w-3.5" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <span className="font-mono text-[10.5px] font-semibold uppercase tracking-[0.18em] text-amber-600 dark:text-amber-400">
              DEV · Demo account
            </span>
          </div>
          <button
            type="button"
            onClick={onPick}
            aria-pressed={active}
            className={cn(
              "mt-2 flex w-full items-center gap-2 rounded-md border px-2.5 py-1.5 text-left",
              "transition-colors",
              active
                ? "border-amber-500/50 bg-amber-500/15"
                : "border-amber-500/30 bg-transparent hover:bg-amber-500/10",
            )}
          >
            <span className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-[var(--color-primary)] text-[10px] font-bold text-[var(--color-primary-foreground)]">
              SA
            </span>
            <span className="min-w-0 flex-1">
              <span className="flex items-center gap-1.5">
                <span className="text-[12.5px] font-semibold tracking-tight">{DEMO_SUPERADMIN.label}</span>
                <span className="rounded-full bg-amber-500/15 px-1.5 py-0.5 font-mono text-[9px] font-medium uppercase tracking-[0.12em] text-amber-600 dark:text-amber-400">
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
                "shrink-0 font-mono text-[10px] uppercase tracking-[0.14em]",
                active ? "text-amber-600 dark:text-amber-400" : "text-[var(--color-muted-foreground)]",
              )}
            >
              {active ? "loaded" : "use →"}
            </span>
          </button>

          <div className="mt-3 flex items-center justify-between text-[11px]">
            <span className="flex items-center gap-1.5 text-[var(--color-muted-foreground)]">
              <ShieldAlert className="h-3 w-3" />
              <span className="font-mono uppercase tracking-[0.14em]">password</span>
              <code className="rounded bg-amber-500/15 px-1.5 py-0.5 font-mono text-[11px] font-semibold text-amber-700 dark:text-amber-300">
                {DEMO_PASSWORD}
              </code>
            </span>
            <button
              type="button"
              onClick={onCopy}
              className={cn(
                "inline-flex h-6 cursor-pointer items-center gap-1 rounded-md px-2 font-mono text-[10px] uppercase tracking-[0.14em]",
                "transition-colors",
                copied
                  ? "bg-emerald-500/15 text-emerald-600 dark:text-emerald-400"
                  : "text-[var(--color-muted-foreground)] hover:bg-amber-500/15 hover:text-amber-600 dark:hover:text-amber-400",
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
