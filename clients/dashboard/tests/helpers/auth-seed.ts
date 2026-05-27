import type { Page } from "@playwright/test";

/**
 * Seed an authenticated session on the page BEFORE React boots, so
 * tests targeting protected routes don't bounce to /login.
 *
 * We populate the same localStorage keys the runtime tokenStore writes
 * (see clients/dashboard/src/auth/token-store.ts). The token value is
 * a JWT-shaped string that decodes to the supplied user — useAuth's
 * decoder reads sub/email/given_name/family_name/tenant/permissions
 * out of the payload.
 */
export type SeededUser = {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  tenant: string;
  permissions?: string[];
};

const ACCESS_KEY = "fsh.dashboard.accessToken";
const REFRESH_KEY = "fsh.dashboard.refreshToken";
const TENANT_KEY = "fsh.dashboard.tenant";

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
  // looks for in the runtime path.
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
