import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "@/auth/use-auth";
import { ForbiddenView } from "@/components/forbidden-view";

type ProtectedRouteProps = {
  /**
   * Optional list of permission strings. When provided, the current user must hold EVERY
   * listed permission. Missing any one renders a 403 view instead of navigating to login.
   * Omit or pass [] to keep the route auth-only (any signed-in user).
   */
  permissions?: string[];
};

export function ProtectedRoute({ permissions = [] }: ProtectedRouteProps) {
  const { isAuthenticated, isInitializing, user } = useAuth();
  const location = useLocation();

  // Resolving a stored session (silent token refresh) — hold rendering so we
  // neither flash a protected surface with a stale/expired token nor bounce to
  // /login before the refresh has had a chance to restore the session.
  if (isInitializing) {
    return (
      <div
        className="flex min-h-screen items-center justify-center text-sm text-[var(--color-muted-foreground)]"
        role="status"
        aria-busy="true"
      >
        <span className="sr-only">Restoring your session…</span>
        <span
          className="size-5 animate-spin rounded-full border-2 border-current border-t-transparent"
          aria-hidden
        />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (permissions.length > 0) {
    const granted = user?.permissions ?? [];
    const missing = permissions.filter((p) => !granted.includes(p));
    if (missing.length > 0) {
      return <ForbiddenView missing={missing} />;
    }
  }

  return <Outlet />;
}
