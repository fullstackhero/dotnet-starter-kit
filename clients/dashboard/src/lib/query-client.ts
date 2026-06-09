import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import {
  ApiRequestError,
  isImpersonationRevokedError,
  isTenantDeactivatedError,
} from "@/lib/api-client";
import { router } from "@/routes";

const TENANT_DEACTIVATED_PATH = "/tenant-deactivated";
const IMPERSONATION_ENDED_PATH = "/impersonation-ended";

// Both terminal states share the same shape: once they trip, *every* request
// fails the same way, so this hook fires from many queries/mutations at once.
// We route from the first occurrence and no-op the rest by guarding on the
// current location. Navigation goes through the data router instance directly
// because this runs outside React. The dead token is intentionally NOT cleared
// here — clearing flips isAuthenticated false and lets ProtectedRoute race us
// to /login; the terminal pages clear it on their "Back to sign in" action.
function handleGlobalError(error: unknown) {
  if (isTenantDeactivatedError(error)) {
    if (router.state.location.pathname === TENANT_DEACTIVATED_PATH) return;
    void router.navigate(TENANT_DEACTIVATED_PATH, { replace: true });
    return;
  }

  if (isImpersonationRevokedError(error)) {
    if (router.state.location.pathname === IMPERSONATION_ENDED_PATH) return;
    // Surface the dev-only rejection reason on the page when present; prod
    // blanks it, so the page copy must stand on its own without it.
    const reason =
      error instanceof ApiRequestError && typeof error.problem?.reason === "string"
        ? error.problem.reason
        : undefined;
    void router.navigate(IMPERSONATION_ENDED_PATH, { replace: true, state: { reason } });
  }
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({ onError: handleGlobalError }),
  mutationCache: new MutationCache({ onError: handleGlobalError }),
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        if (error instanceof ApiRequestError && (error.status === 401 || error.status === 403)) {
          return false;
        }
        return failureCount < 2;
      },
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
});
