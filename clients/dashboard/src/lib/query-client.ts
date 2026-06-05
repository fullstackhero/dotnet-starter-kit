import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import { ApiRequestError, isTenantDeactivatedError } from "@/lib/api-client";
import { router } from "@/routes";

const TENANT_DEACTIVATED_PATH = "/tenant-deactivated";

// Once a tenant is deactivated every request 403s, so this hook can fire from
// many queries/mutations at once. Route to the dedicated page from the first
// one and no-op the rest by guarding on the current location. Navigation goes
// through the data router instance directly because this runs outside React.
function handleGlobalError(error: unknown) {
  if (!isTenantDeactivatedError(error)) return;
  if (router.state.location.pathname === TENANT_DEACTIVATED_PATH) return;
  void router.navigate(TENANT_DEACTIVATED_PATH, { replace: true });
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
