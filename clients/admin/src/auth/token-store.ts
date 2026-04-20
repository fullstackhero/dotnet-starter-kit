const ACCESS_KEY = "fsh.admin.accessToken";
const REFRESH_KEY = "fsh.admin.refreshToken";
const TENANT_KEY = "fsh.admin.tenant";

type Listener = () => void;

const listeners = new Set<Listener>();

function emit() {
  for (const listener of listeners) listener();
}

export const tokenStore = {
  getAccessToken: () => localStorage.getItem(ACCESS_KEY),
  getRefreshToken: () => localStorage.getItem(REFRESH_KEY),
  getTenant: () => localStorage.getItem(TENANT_KEY),

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
    emit();
  },

  subscribe(listener: Listener) {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};
