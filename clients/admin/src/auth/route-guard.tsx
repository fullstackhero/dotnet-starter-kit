import type { ReactNode } from "react";
import { useAuth } from "@/auth/use-auth";
import { ForbiddenView } from "@/components/forbidden-view";

type RouteGuardProps = {
  /**
   * Permission strings the current user must hold to view the wrapped
   * content. Missing any one renders ForbiddenView instead of the route.
   * Use this on the route's `element` to gate by permission per-page —
   * ProtectedRoute itself only handles the auth-vs-anonymous question.
   */
  perms: readonly string[];
  children: ReactNode;
};

/**
 * RouteGuard — per-route permission wrapper. Layered inside ProtectedRoute,
 * not as a replacement: ProtectedRoute decides "are you signed in?", this
 * decides "do you hold these specific permissions for this surface?".
 *
 * Note: the JWT only carries role names; permissions are resolved server-side
 * per role and fetched into AuthContext after sign-in. While that fetch is in
 * flight (permissionsHydrated=false), we render a quiet loading slug instead
 * of 403 to avoid a flash of "access denied" on first paint.
 */
export function RouteGuard({ perms, children }: RouteGuardProps) {
  const { user, permissionsHydrated } = useAuth();

  if (!permissionsHydrated) {
    return (
      <div
        className="flex min-h-[60vh] items-center justify-center text-sm font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]"
        aria-busy
      >
        Resolving permissions
        <span className="caret text-[var(--color-accent-signal)]" aria-hidden />
      </div>
    );
  }

  const granted = user?.permissions ?? [];
  const missing = perms.filter((p) => !granted.includes(p));

  if (missing.length > 0) {
    return <ForbiddenView missing={missing} />;
  }

  return <>{children}</>;
}
