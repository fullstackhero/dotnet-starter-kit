const ACCESS_KEY = "fsh.dashboard.accessToken";
const REFRESH_KEY = "fsh.dashboard.refreshToken";
const TENANT_KEY = "fsh.dashboard.tenant";
const PERMS_KEY = "fsh.dashboard.permissions";

// Impersonation stash. While an operator is impersonating another user,
// the live token store holds the impersonation tokens; the operator's
// original tokens sit under these keys so the End flow can fall back to
// them locally if the server-side End call fails (e.g. network down).
const STASH_ACCESS_KEY = "fsh.dashboard.impersonation.actorAccessToken";
const STASH_REFRESH_KEY = "fsh.dashboard.impersonation.actorRefreshToken";
const STASH_TENANT_KEY = "fsh.dashboard.impersonation.actorTenant";

type Listener = () => void;

const listeners = new Set<Listener>();

function emit() {
  for (const listener of listeners) listener();
}

export const tokenStore = {
  getAccessToken: () => localStorage.getItem(ACCESS_KEY),
  getRefreshToken: () => localStorage.getItem(REFRESH_KEY),
  getTenant: () => localStorage.getItem(TENANT_KEY),

  /**
   * Permissions are fetched separately from the JWT (the token only carries
   * role names — see GetCurrentUserPermissionsEndpoint server-side). Cached
   * here so gated UI can read them synchronously; re-hydrated on each login
   * and whenever the signed-in subject changes (incl. impersonation swaps).
   */
  getPermissions(): string[] {
    try {
      const raw = localStorage.getItem(PERMS_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as unknown;
      return Array.isArray(parsed) ? parsed.filter((p): p is string => typeof p === "string") : [];
    } catch {
      return [];
    }
  },

  setPermissions(permissions: string[]) {
    localStorage.setItem(PERMS_KEY, JSON.stringify(permissions));
    emit();
  },

  setTokens(accessToken: string, refreshToken: string) {
    localStorage.setItem(ACCESS_KEY, accessToken);
    localStorage.setItem(REFRESH_KEY, refreshToken);
    emit();
  },

  setTenant(tenant: string) {
    localStorage.setItem(TENANT_KEY, tenant);
    emit();
  },

  clear() {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(PERMS_KEY);
    // Also clear any impersonation stash so a fresh login doesn't
    // inherit half of a previous operator's session.
    localStorage.removeItem(STASH_ACCESS_KEY);
    localStorage.removeItem(STASH_REFRESH_KEY);
    localStorage.removeItem(STASH_TENANT_KEY);
    emit();
  },

  /**
   * Swap the active token to an impersonation access token while preserving
   * the original operator's tokens locally. The impersonation token has no
   * refresh counterpart server-side, so we drop the refresh slot — auto-
   * refresh in the api client checks for refreshToken presence and will
   * skip silently (impersonation sessions are intentionally short-lived).
   */
  beginImpersonation(impersonationAccessToken: string, impersonatedTenant: string | null) {
    const access = localStorage.getItem(ACCESS_KEY);
    const refresh = localStorage.getItem(REFRESH_KEY);
    const tenant = localStorage.getItem(TENANT_KEY);
    if (access) localStorage.setItem(STASH_ACCESS_KEY, access);
    if (refresh) localStorage.setItem(STASH_REFRESH_KEY, refresh);
    if (tenant) localStorage.setItem(STASH_TENANT_KEY, tenant);

    localStorage.setItem(ACCESS_KEY, impersonationAccessToken);
    localStorage.removeItem(REFRESH_KEY);
    // Drop the operator's permissions — the impersonated subject has its own;
    // the auth context re-hydrates on the subject change.
    localStorage.removeItem(PERMS_KEY);
    if (impersonatedTenant) localStorage.setItem(TENANT_KEY, impersonatedTenant);
    emit();
  },

  /**
   * Replace the live tokens with a fresh actor pair returned by the End
   * Impersonation endpoint, and clear the stash. Use this on End success.
   */
  endImpersonationWithFreshTokens(accessToken: string, refreshToken: string) {
    const stashTenant = localStorage.getItem(STASH_TENANT_KEY);
    localStorage.setItem(ACCESS_KEY, accessToken);
    localStorage.setItem(REFRESH_KEY, refreshToken);
    localStorage.removeItem(PERMS_KEY);
    if (stashTenant) localStorage.setItem(TENANT_KEY, stashTenant);
    localStorage.removeItem(STASH_ACCESS_KEY);
    localStorage.removeItem(STASH_REFRESH_KEY);
    localStorage.removeItem(STASH_TENANT_KEY);
    emit();
  },

  /**
   * Last-resort local restore — used if the End endpoint fails. Reinstall
   * the stashed actor tokens so the operator at least has *some* session
   * (the original access token may itself be expired by now, in which
   * case auto-refresh with the stashed refresh token kicks in).
   */
  restoreStashedActor(): boolean {
    const access = localStorage.getItem(STASH_ACCESS_KEY);
    const refresh = localStorage.getItem(STASH_REFRESH_KEY);
    const tenant = localStorage.getItem(STASH_TENANT_KEY);
    if (!access) return false;
    localStorage.setItem(ACCESS_KEY, access);
    if (refresh) localStorage.setItem(REFRESH_KEY, refresh);
    localStorage.removeItem(PERMS_KEY);
    if (tenant) localStorage.setItem(TENANT_KEY, tenant);
    localStorage.removeItem(STASH_ACCESS_KEY);
    localStorage.removeItem(STASH_REFRESH_KEY);
    localStorage.removeItem(STASH_TENANT_KEY);
    emit();
    return true;
  },

  hasImpersonationStash: () => localStorage.getItem(STASH_ACCESS_KEY) !== null,

  subscribe(listener: Listener) {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};
