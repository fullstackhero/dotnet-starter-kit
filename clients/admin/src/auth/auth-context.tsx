import { createContext, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { tokenStore } from "@/auth/token-store";
import { decodeJwt, isTokenExpired, type JwtClaims } from "@/auth/jwt";
import { issueToken } from "@/auth/api";
import { refreshAccessToken } from "@/lib/api-client";
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
   * True while the provider resolves a stored session at boot — the access
   * token was missing/expired but a refresh token was present, so a silent
   * refresh is in flight. Routes render a loader (not the page, not a redirect)
   * while this is true, so a stale token never flashes a protected surface.
   */
  isInitializing: boolean;
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

// Read the stored session, treating an EXPIRED access token as "not usable
// yet": the boot effect attempts a silent refresh before we trust it. A
// decodable-but-expired token must not flip the app to authenticated, or it
// renders protected surfaces that 401 in a loop instead of refreshing.
function readStoredSession(): { claims: JwtClaims | null; usable: boolean } {
  const claims = decodeJwt(tokenStore.getAccessToken());
  return { claims, usable: claims !== null && !isTokenExpired(claims) };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<AuthUser | null>(() => {
    const { claims, usable } = readStoredSession();
    return usable ? claimsToUser(claims, tokenStore.getPermissions()) : null;
  });
  // When the stored access token is missing/expired but a refresh token is
  // present, attempt one silent refresh at boot before rendering — keeps
  // long-lived sessions alive AND stops a stale token from flashing a doomed
  // protected surface that fires 401-ing requests.
  const [isInitializing, setIsInitializing] = useState<boolean>(
    () => !readStoredSession().usable && tokenStore.getRefreshToken() !== null,
  );
  // Cold-start: if we already have a cached permissions list, treat as hydrated
  // so route guards don't flash 403. Otherwise, wait for the effect.
  const [permissionsHydrated, setPermissionsHydrated] = useState<boolean>(() => {
    if (!tokenStore.getAccessToken()) return true;
    return tokenStore.getPermissions().length > 0;
  });
  const lastHydratedSubject = useRef<string | null>(user?.id ?? null);

  useEffect(() => {
    if (!isInitializing) return;
    let cancelled = false;
    void (async () => {
      try {
        await refreshAccessToken();
      } catch {
        // Refresh token dead (expired, revoked, or DB reseeded) — drop the
        // stale session so routing falls through to /login cleanly.
        tokenStore.clear();
      } finally {
        if (!cancelled) setIsInitializing(false);
      }
    })();
    return () => {
      cancelled = true;
    };
    // Boot-only: isInitializing only ever flips false, never back on.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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

  // Cross-tab auth sync. tokenStore.subscribe only fires for in-app mutations;
  // a `storage` event fires when ANOTHER tab logs in/out (e.g. inactivity
  // sign-out). Rebuild from the (now changed) tokens so a logout in one tab
  // drops every tab to /login. Scoped to the token keys so the inactivity
  // heartbeat ("fsh.lastActivity") doesn't trigger a rebuild every second.
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (
        e.key !== null &&
        e.key !== "fsh.admin.accessToken" &&
        e.key !== "fsh.admin.refreshToken"
      ) {
        return;
      }
      setUser(claimsToUser(decodeJwt(tokenStore.getAccessToken()), tokenStore.getPermissions()));
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
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
      isInitializing,
      permissionsHydrated,
      login,
      logout,
      refreshPermissions,
    }),
    [user, isInitializing, permissionsHydrated, login, logout, refreshPermissions],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
