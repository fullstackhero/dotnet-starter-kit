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
    claimsToUser(decodeJwt(tokenStore.getAccessToken())),
  );

  useEffect(() => {
    return tokenStore.subscribe(() => {
      setUser(claimsToUser(decodeJwt(tokenStore.getAccessToken())));
    });
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
