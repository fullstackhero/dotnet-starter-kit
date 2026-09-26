import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "@/auth/use-auth";

export function ProtectedRoute() {
  const { t } = useTranslation("common");
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  // Resolving a stored session (silent token refresh) — hold rendering so we
  // neither flash the dashboard with a stale/expired token nor bounce to
  // /login before the refresh has had a chance to restore the session.
  if (isInitializing) {
    return (
      <div
        className="flex min-h-screen items-center justify-center text-sm text-muted-foreground"
        role="status"
        aria-busy="true"
      >
        <span className="sr-only">{t("session.restoring")}</span>
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

  return <Outlet />;
}
