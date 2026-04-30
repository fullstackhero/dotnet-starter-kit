import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { tokenStore } from "@/auth/token-store";
import { decodeJwt, type JwtClaims } from "@/auth/jwt";
import { issueToken } from "@/auth/api";

export type AuthUser = {
  id: string;
  email?: string;
  name?: string;
  tenant?: string;
  permissions: string[];
};

export type AuthContextValue = {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (input: { email: string; password: string; tenant: string }) => Promise<void>;
  logout: () => void;
};

export const AuthContext = createContext<AuthContextValue | null>(null);

function claimsToUser(claims: JwtClaims | null): AuthUser | null {
  if (!claims?.sub) return null;
  const permissions = Array.isArray(claims.permissions)
    ? claims.permissions
    : typeof claims.permissions === "string"
      ? [claims.permissions]
      : [];
  // `name` is the standard short claim; `unique_name` is what
  // JwtSecurityTokenHandler emits for ClaimTypes.Name. Treat empty
  // strings as missing so the topbar falls through to email/Unknown
  // instead of rendering blank.
  const name = pickFirstNonEmpty(claims.name, claims.unique_name);
  const email = pickFirstNonEmpty(claims.email);
  return {
    id: claims.sub,
    email,
    name,
    tenant: claims.tenant,
    permissions,
  };
}

function pickFirstNonEmpty(...candidates: Array<string | undefined>): string | undefined {
  for (const c of candidates) {
    if (typeof c === "string" && c.trim().length > 0) return c;
  }
  return undefined;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<AuthUser | null>(() =>
    claimsToUser(decodeJwt(tokenStore.getAccessToken())),
  );

  useEffect(() => {
    const refresh = () => setUser(claimsToUser(decodeJwt(tokenStore.getAccessToken())));
    const unsubscribe = tokenStore.subscribe(refresh);

    // The token store's subscribe() only fires for in-app mutations. Storage
    // changes from another tab fire a `storage` event, and same-tab manual
    // clears (e.g. via DevTools) need to be picked up when the user returns
    // to the tab — otherwise `isAuthenticated` stays true while the token
    // is gone, and protected requests silently 401 with no header attached.
    const onStorage = (e: StorageEvent) => {
      if (e.key === null || e.key.startsWith("fsh.dashboard.")) refresh();
    };
    const onVisibility = () => {
      if (document.visibilityState === "visible") refresh();
    };
    window.addEventListener("storage", onStorage);
    document.addEventListener("visibilitychange", onVisibility);

    return () => {
      unsubscribe();
      window.removeEventListener("storage", onStorage);
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, []);

  const login = useCallback(
    async (input: { email: string; password: string; tenant: string }) => {
      tokenStore.setTenant(input.tenant);
      const tokens = await issueToken(input);
      tokenStore.setTokens(tokens.accessToken, tokens.refreshToken);
    },
    [],
  );

  const logout = useCallback(() => {
    tokenStore.clear();
    queryClient.clear();
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      login,
      logout,
    }),
    [user, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
