export type JwtClaims = {
  sub?: string;
  email?: string;
  name?: string;
  /**
   * `unique_name` is what JwtSecurityTokenHandler emits when the source
   * is `ClaimTypes.Name` and no explicit `name` claim is also added.
   * Read as a fallback so tokens issued without a registered `name`
   * claim still resolve a username.
   */
  unique_name?: string;
  tenant?: string;
  permissions?: string[] | string;
  exp?: number;
  /**
   * Actor claims set by the StartImpersonation flow. When present, the
   * current access token represents an impersonation session — the
   * `sub`/`tenant` claims describe the impersonated user, and these
   * `act_*` claims preserve the original operator's identity so the
   * EndImpersonation endpoint can swap back without re-authenticating.
   */
  act_sub?: string;
  act_tenant?: string;
  act_name?: string;
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
