import { createContext, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { tokenStore } from "@/auth/token-store";
import { decodeJwt, isTokenExpired, type JwtClaims } from "@/auth/jwt";
import { issueToken } from "@/auth/api";
import { refreshAccessToken } from "@/lib/api-client";
import { endImpersonation, getMyPermissions, startImpersonation } from "@/api/identity";

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
  /**
   * True once permissions have been fetched at least once for the current
   * subject (or no user is signed in). Lets gated UI avoid flashing while the
   * permissions request is still in flight.
   */
  permissionsHydrated: boolean;
  /** Truthy iff the current access token carries act_sub (impersonation mode). */
  impersonation: ImpersonationInfo | null;
  login: (input: { email: string; password: string; tenant: string }) => Promise<void>;
  logout: () => void;
  /** Re-fetch the permission set for the signed-in user (e.g. after a role change). */
  refreshPermissions: () => Promise<void>;
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

// Permissions are NOT in the JWT (it carries only role names). They're fetched
// from /api/v1/identity/permissions and cached in the token store; this builds
// the user from the token's identity claims + that separately-hydrated list.
function claimsToUser(claims: JwtClaims | null, permissions: string[]): AuthUser | null {
  if (!claims?.sub) return null;
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
    return usable ? claimsToUser(claims, tokenStore.getPermissions()) : null;
  });
  // Hydrated once permissions have been fetched for the current subject. Seed
  // from any cached list so a warm reload doesn't flash ungated UI.
  const [permissionsHydrated, setPermissionsHydrated] = useState<boolean>(() => {
    if (!tokenStore.getAccessToken()) return true;
    return tokenStore.getPermissions().length > 0;
  });
  const lastHydratedSubject = useRef<string | null>(null);
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

  // Hydrate (or re-hydrate) the permission list from the server whenever the
  // signed-in subject changes — covers cold-start, login, and impersonation
  // swaps. Permissions live server-side per role, not in the JWT.
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
        // setPermissions emits → the subscribe listener rebuilds `user` with the list.
        tokenStore.setPermissions(perms);
        setPermissionsHydrated(true);
      } catch {
        // A fetch failure must not sign the user out — gated UI just stays
        // hidden until the next successful hydration.
        if (!cancelled) setPermissionsHydrated(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [user, permissionsHydrated]);

  useEffect(() => {
    const refresh = () => {
      const claims = decodeJwt(tokenStore.getAccessToken());
      setUser(claimsToUser(claims, tokenStore.getPermissions()));
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
      // Stale permissions from a previous user must not leak into the new
      // session — clear before issuing the token so the hydration effect
      // re-fetches from scratch.
      tokenStore.setPermissions([]);
      setPermissionsHydrated(false);
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
    // dashboard session to return to. The local impersonation token is
    // disposable, so end INSTANTLY by logging out — don't block the UI on the
    // server `end` call (it has a 30s timeout and intermittently leaves the
    // banner stuck on "Ending…"). Fire it best-effort for grant revocation +
    // audit; the short-lived impersonation token expires shortly regardless.
    // Restoring the operator here would also drop a root-tenant account into
    // the tenant dashboard, which `login` forbids.
    if (!tokenStore.hasImpersonationStash()) {
      void endImpersonation().catch(() => {
        /* best-effort: token expires shortly, nothing to recover here */
      });
      logout();
      return;
    }

    // Intra-app impersonation: we genuinely need the server-minted operator
    // tokens to restore the original dashboard session, so await the call.
    try {
      const fresh = await endImpersonation();
      tokenStore.endImpersonationWithFreshTokens(fresh.accessToken, fresh.refreshToken);
    } catch {
      // End endpoint failed (server unreachable / token invalid). Fall
      // back to whatever we stashed locally; the operator may need to
      // re-authenticate if the stashed access token has expired.
      tokenStore.restoreStashedActor();
      throw new Error("End impersonation failed; restored local session.");
    } finally {
      queryClient.clear();
    }
  }, [queryClient, logout]);

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
      impersonation,
      login,
      logout,
      beginImpersonation,
      stopImpersonation,
      refreshPermissions,
    }),
    [user, isInitializing, permissionsHydrated, impersonation, login, logout, beginImpersonation, stopImpersonation, refreshPermissions],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
