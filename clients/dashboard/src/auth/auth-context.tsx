import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { tokenStore } from "@/auth/token-store";
import { decodeJwt, isTokenExpired, type JwtClaims } from "@/auth/jwt";
import { issueToken } from "@/auth/api";
import { refreshAccessToken } from "@/lib/api-client";
import { endImpersonation, startImpersonation } from "@/api/identity";

export type AuthUser = {
  id: string;
  email?: string;
  name?: string;
  tenant?: string;
  permissions: string[];
};

export type ImpersonationInfo = {
  /** The original operator's user id, taken from the act_sub claim. */
  actorUserId: string;
  /** The original operator's tenant, taken from the act_tenant claim. */
  actorTenant?: string;
  /** Display name for the original operator if the token carries act_name. */
  actorName?: string;
};

export type AuthContextValue = {
  user: AuthUser | null;
  isAuthenticated: boolean;
  /**
   * True while the provider resolves a stored session at boot — the access
   * token was missing/expired but a refresh token was present, so a silent
   * refresh is in flight. Routes render a loader (not the page, not a redirect)
   * while this is true, so a stale token never flashes a doomed dashboard.
   */
  isInitializing: boolean;
  /** Truthy iff the current access token carries act_sub (impersonation mode). */
  impersonation: ImpersonationInfo | null;
  login: (input: { email: string; password: string; tenant: string }) => Promise<void>;
  logout: () => void;
  /** Begin impersonating another user. Resolves once the new token is installed. */
  beginImpersonation: (input: {
    targetUserId: string;
    targetTenantId: string;
    reason?: string;
  }) => Promise<void>;
  /** Stop impersonating and restore the operator session. */
  stopImpersonation: () => Promise<void>;
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

function claimsToImpersonation(claims: JwtClaims | null): ImpersonationInfo | null {
  if (!claims?.act_sub) return null;
  return {
    actorUserId: claims.act_sub,
    actorTenant: claims.act_tenant,
    actorName: claims.act_name,
  };
}

function pickFirstNonEmpty(...candidates: Array<string | undefined>): string | undefined {
  for (const c of candidates) {
    if (typeof c === "string" && c.trim().length > 0) return c;
  }
  return undefined;
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
    return usable ? claimsToUser(claims) : null;
  });
  const [impersonation, setImpersonation] = useState<ImpersonationInfo | null>(() => {
    const { claims, usable } = readStoredSession();
    return usable ? claimsToImpersonation(claims) : null;
  });
  // When the stored access token is missing/expired but a refresh token is
  // present, attempt one silent refresh at boot before rendering — this keeps
  // long-lived sessions alive (45-min access / 7-day refresh) AND stops a stale
  // token from flashing a doomed dashboard that fires 401-ing requests.
  const [isInitializing, setIsInitializing] = useState<boolean>(
    () => !readStoredSession().usable && tokenStore.getRefreshToken() !== null,
  );

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

  useEffect(() => {
    const refresh = () => {
      const claims = decodeJwt(tokenStore.getAccessToken());
      setUser(claimsToUser(claims));
      setImpersonation(claimsToImpersonation(claims));
    };
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
      // Defence-in-depth: even though the API rejects root-tenant logins
      // submitted with X-FSH-App=dashboard, double-check the issued token
      // so a future API regression can't quietly drop a root token into
      // a tenant-dashboard session.
      const claims = decodeJwt(tokens.accessToken);
      if (claims?.tenant === "root") {
        tokenStore.clear();
        throw new Error(
          "SuperAdmin accounts must use the admin app. Sign in there instead.",
        );
      }
      tokenStore.setTokens(tokens.accessToken, tokens.refreshToken);
      // Drop any cached query state from before login. Without this, a
      // failed pre-login probe (e.g. OverviewPage's billing fetch
      // firing during the brief window before ProtectedRoute redirects
      // to /login, or a stale error from a previous session) sticks in
      // the react-query cache as a 401 and renders as an ErrorBand on
      // the next page — react-query's retry config blocks auto-retries
      // for 401, so the stale error would never refetch on its own.
      queryClient.clear();
    },
    [queryClient],
  );

  const logout = useCallback(() => {
    tokenStore.clear();
    queryClient.clear();
  }, [queryClient]);

  const beginImpersonation = useCallback(
    async (input: { targetUserId: string; targetTenantId: string; reason?: string }) => {
      const response = await startImpersonation(input);
      // Swap the active token. queryClient.clear() drops cached queries
      // so the next render fetches with the new identity — otherwise
      // user/role/permission caches from the actor session would leak.
      tokenStore.beginImpersonation(response.accessToken, response.impersonatedTenantId);
      queryClient.clear();
    },
    [queryClient],
  );

  const stopImpersonation = useCallback(async () => {
    // No stash ⇒ the operator arrived via a cross-app handoff (e.g. a root
    // SuperAdmin started impersonation from the admin app), so there is no
    // dashboard session to return to. Restoring the operator here would drop a
    // root-tenant account into the tenant dashboard — which `login` explicitly
    // forbids — so end cleanly by logging out instead.
    const crossAppHandoff = !tokenStore.hasImpersonationStash();
    try {
      // Still call the server so the impersonation grant is ended + audited.
      const fresh = await endImpersonation();
      if (crossAppHandoff) {
        logout();
        return;
      }
      tokenStore.endImpersonationWithFreshTokens(fresh.accessToken, fresh.refreshToken);
    } catch {
      if (crossAppHandoff) {
        logout();
        return;
      }
      // End endpoint failed (server unreachable / token invalid). Fall
      // back to whatever we stashed locally; the operator may need to
      // re-authenticate if the stashed access token has expired.
      tokenStore.restoreStashedActor();
      throw new Error("End impersonation failed; restored local session.");
    } finally {
      queryClient.clear();
    }
  }, [queryClient, logout]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isInitializing,
      impersonation,
      login,
      logout,
      beginImpersonation,
      stopImpersonation,
    }),
    [user, isInitializing, impersonation, login, logout, beginImpersonation, stopImpersonation],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
