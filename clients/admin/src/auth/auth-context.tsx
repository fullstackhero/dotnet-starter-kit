import { createContext, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { tokenStore } from "@/auth/token-store";
import { decodeJwt, type JwtClaims } from "@/auth/jwt";
import { issueToken } from "@/auth/api";
import { getMyPermissions } from "@/api/users";

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
  /**
   * True once permissions have been fetched at least once for the current
   * user (or no user is signed in). Route guards check this before rendering
   * a 403 — without it, the first paint flashes "access denied" while the
   * permissions request is still in flight.
   */
  permissionsHydrated: boolean;
  login: (input: { email: string; password: string; tenant: string }) => Promise<void>;
  logout: () => void;
  /** Re-fetch the permission set for the signed-in user. Call after a role
   *  assignment changes for the current user. */
  refreshPermissions: () => Promise<void>;
};

export const AuthContext = createContext<AuthContextValue | null>(null);

function claimsToUser(claims: JwtClaims | null, permissions: string[]): AuthUser | null {
  if (!claims?.sub) return null;
  return {
    id: claims.sub,
    email: claims.email,
    name: claims.name,
    tenant: claims.tenant,
    permissions,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<AuthUser | null>(() =>
    claimsToUser(decodeJwt(tokenStore.getAccessToken()), tokenStore.getPermissions()),
  );
  // Cold-start: if we already have a cached permissions list, treat as hydrated
  // so route guards don't flash 403. Otherwise, wait for the effect.
  const [permissionsHydrated, setPermissionsHydrated] = useState<boolean>(() => {
    if (!tokenStore.getAccessToken()) return true;
    return tokenStore.getPermissions().length > 0;
  });
  const lastHydratedSubject = useRef<string | null>(user?.id ?? null);

  // Hydrate (or re-hydrate) the permissions list from the server whenever the
  // signed-in subject changes — covers cold-start, login, and account swap.
  useEffect(() => {
    if (!user) {
      lastHydratedSubject.current = null;
      setPermissionsHydrated(true);
      return;
    }
    if (lastHydratedSubject.current === user.id && permissionsHydrated) {
      return;
    }
    lastHydratedSubject.current = user.id;
    let cancelled = false;
    void (async () => {
      try {
        const perms = await getMyPermissions();
        if (cancelled) return;
        tokenStore.setPermissions(perms);
        // setPermissions emits, the subscribe listener will rebuild `user`
        // with the new list.
        setPermissionsHydrated(true);
      } catch {
        // Permissions fetch failure shouldn't sign the user out — the route
        // guards will treat them as zero-permission until the next refresh.
        if (!cancelled) setPermissionsHydrated(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [user, permissionsHydrated]);

  useEffect(() => {
    return tokenStore.subscribe(() => {
      const next = claimsToUser(decodeJwt(tokenStore.getAccessToken()), tokenStore.getPermissions());
      setUser(next);
    });
  }, []);

  const login = useCallback(
    async (input: { email: string; password: string; tenant: string }) => {
      tokenStore.setTenant(input.tenant);
      // Stale permissions from a previous user must not leak into the new
      // session — clear before issuing the token so the hydration effect
      // re-fetches from scratch.
      tokenStore.setPermissions([]);
      setPermissionsHydrated(false);
      const tokens = await issueToken(input);
      tokenStore.setTokens(tokens.accessToken, tokens.refreshToken);
    },
    [],
  );

  const logout = useCallback(() => {
    tokenStore.clear();
    queryClient.clear();
  }, [queryClient]);

  const refreshPermissions = useCallback(async () => {
    try {
      const perms = await getMyPermissions();
      tokenStore.setPermissions(perms);
    } catch {
      /* swallow — see hydration effect */
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      permissionsHydrated,
      login,
      logout,
      refreshPermissions,
    }),
    [user, permissionsHydrated, login, logout, refreshPermissions],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
