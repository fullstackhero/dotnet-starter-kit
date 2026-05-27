const ACCESS_KEY = "fsh.admin.accessToken";
const REFRESH_KEY = "fsh.admin.refreshToken";
const TENANT_KEY = "fsh.admin.tenant";
const PERMS_KEY = "fsh.admin.permissions";

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
   * here so route guards can read them synchronously; refreshed on each login.
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
    emit();
  },

  subscribe(listener: Listener) {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};
