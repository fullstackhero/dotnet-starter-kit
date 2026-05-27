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
  const { isAuthenticated, user } = useAuth();
  const location = useLocation();

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
