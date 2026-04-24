import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";

type LocationState = { from?: { pathname: string } };

// Dev-only seeded credentials — match what IdentityDbInitializer creates for the root tenant.
// Surfaced as a one-click button below; never shipped in production bundles because Vite
// statically replaces import.meta.env.DEV with false during `vite build`, so the entire
// branch is dead-code-eliminated.
const DEFAULT_DEV_CREDENTIALS = {
  email: "admin@root.com",
  password: "123Pa$$word!",
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

  const onSignInAsDefault = async () => {
    await performLogin({
      email: DEFAULT_DEV_CREDENTIALS.email,
      password: DEFAULT_DEV_CREDENTIALS.password,
      tenant: env.defaultTenant,
    });
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-[var(--color-background)] p-6">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>Sign in</CardTitle>
          <CardDescription>FullStackHero Dashboard</CardDescription>
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
          <CardFooter className="flex flex-col gap-2">
            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting ? "Signing in…" : "Sign in"}
            </Button>
            {import.meta.env.DEV && (
              <Button
                type="button"
                variant="outline"
                className="w-full"
                disabled={submitting}
                onClick={onSignInAsDefault}
              >
                Sign in as default (dev only)
              </Button>
            )}
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
