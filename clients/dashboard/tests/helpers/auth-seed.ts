import type { Page } from "@playwright/test";

/**
 * Seed an authenticated session on the page BEFORE React boots, so
 * tests targeting protected routes don't bounce to /login.
 *
 * We populate the same localStorage keys the runtime tokenStore writes
 * (see clients/dashboard/src/auth/token-store.ts). The token value is
 * a JWT-shaped string that decodes to the supplied user — useAuth's
 * decoder reads sub/email/given_name/family_name/tenant out of the
 * payload. Permissions are NOT read from the JWT: at runtime the app
 * fetches them from GET /api/v1/identity/permissions (installShellMocks
 * stubs that route to []; permission-gated specs re-mock it with the
 * grants they need — see tests/system/trash.spec.ts).
 */
export type SeededUser = {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  tenant: string;
  /**
   * Inert — written into the fake JWT payload but ignored by the app.
   * To grant permissions in a spec, mock GET /identity/permissions
   * instead.
   */
  permissions?: string[];
};

/** The localStorage slots the runtime tokenStore writes (token-store.ts). */
export const ACCESS_KEY = "fsh.dashboard.accessToken";
export const REFRESH_KEY = "fsh.dashboard.refreshToken";
export const TENANT_KEY = "fsh.dashboard.tenant";

/**
 * Encode a minimal JWT (header.payload.signature) where every segment is
 * base64url-encoded JSON. Signature is a junk string — the dashboard
 * never validates it (only the server does), so this is safe.
 */
function fakeJwt(payload: Record<string, unknown>): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  return [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");
}

export async function seedAuthedSession(page: Page, user: SeededUser) {
  // Build the JWT-shaped payload. Claim names match what useAuth's decoder
  // looks for in the runtime path (permissions excepted — inert, see SeededUser).
  const payload = {
    sub: user.sub,
    email: user.email,
    given_name: user.firstName,
    family_name: user.lastName,
    name: `${user.firstName} ${user.lastName}`.trim(),
    tenant: user.tenant,
    permissions: user.permissions ?? [],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  };
  const accessToken = fakeJwt(payload);

  await page.addInitScript(
    ({ access, refresh, tenant, accessKey, refreshKey, tenantKey }) => {
      localStorage.setItem(accessKey, access);
      localStorage.setItem(refreshKey, refresh);
      localStorage.setItem(tenantKey, tenant);
    },
    {
      access: accessToken,
      refresh: "fake-refresh-token",
      tenant: user.tenant,
      accessKey: ACCESS_KEY,
      refreshKey: REFRESH_KEY,
      tenantKey: TENANT_KEY,
    },
  );
}

/** The original operator behind an impersonation session (the `act_*` claims). */
export type ImpersonationActor = {
  userId: string;
  tenant: string;
  name: string;
};

/**
 * Seed an IMPERSONATION session into localStorage before React boots: an access
 * token carrying the `act_sub` actor claim, the target's tenant, and —
 * critically — NO refresh token (token-store drops the refresh slot on
 * beginImpersonation). The missing refresh token is what makes a 401 propagate
 * to the global error hook rather than triggering a silent refresh-and-retry.
 */
export async function seedImpersonationSession(
  page: Page,
  user: SeededUser,
  actor: ImpersonationActor,
) {
  const accessToken = fakeJwt({
    sub: user.sub,
    email: user.email,
    name: `${user.firstName} ${user.lastName}`.trim(),
    tenant: user.tenant,
    // Actor claims — the original operator's identity. Their presence is what
    // marks this token as an impersonation session.
    act_sub: actor.userId,
    act_tenant: actor.tenant,
    act_name: actor.name,
    permissions: user.permissions ?? [],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  });

  await page.addInitScript(
    ({ access, tenant, accessKey, refreshKey, tenantKey }) => {
      localStorage.setItem(accessKey, access);
      // Defensive: ensure no refresh token lingers from a prior session.
      localStorage.removeItem(refreshKey);
      localStorage.setItem(tenantKey, tenant);
    },
    {
      access: accessToken,
      tenant: user.tenant,
      accessKey: ACCESS_KEY,
      refreshKey: REFRESH_KEY,
      tenantKey: TENANT_KEY,
    },
  );
}

/**
 * Default test user — keep this consistent across specs so the captured
 * profile-id assertions stay stable.
 */
export const TEST_USER: SeededUser = {
  sub: "u-test-1",
  email: "alice@acme.com",
  firstName: "Alice",
  lastName: "Nguyen",
  tenant: "acme",
  permissions: [],
};

/** The subject an operator impersonates in the impersonation specs. */
export const IMPERSONATED_USER: SeededUser = {
  sub: "u-impersonated-1",
  email: "dan@acme.com",
  firstName: "Dan",
  lastName: "Mueller",
  tenant: "acme",
  permissions: [],
};

/** The operator driving the impersonation specs. */
export const OPERATOR_ACTOR: ImpersonationActor = {
  userId: "op-root-1",
  tenant: "root",
  name: "Root Operator",
};
