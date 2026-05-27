export type JwtClaims = {
  sub?: string;
  email?: string;
  name?: string;
  tenant?: string;
  permissions?: string[] | string;
  exp?: number;
  [key: string]: unknown;
};

export function decodeJwt(token: string | null | undefined): JwtClaims | null {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = payload + "=".repeat((4 - (payload.length % 4)) % 4);
    const json = atob(padded);
    return JSON.parse(json) as JwtClaims;
  } catch {
    return null;
  }
}

/**
 * True when the token is expired, or within `skewMs` of expiring (so we refresh
 * proactively rather than send a request the server will reject for an
 * about-to-die token). A token with no `exp` claim is treated as non-expiring.
 *
 * Used at boot to decide whether a stored access token still represents a
 * usable session: a decodable-but-expired token must NOT count as "signed in",
 * or the app renders protected surfaces that 401 in a loop instead of
 * refreshing or routing to /login.
 */
export function isTokenExpired(claims: JwtClaims | null, skewMs = 10_000): boolean {
  if (!claims || typeof claims.exp !== "number") return false;
  return claims.exp * 1000 <= Date.now() + skewMs;
}
