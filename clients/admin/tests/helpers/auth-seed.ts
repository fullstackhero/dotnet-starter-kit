import type { Page } from "@playwright/test";

const ACCESS_KEY = "fsh.admin.accessToken";
const REFRESH_KEY = "fsh.admin.refreshToken";
const TENANT_KEY = "fsh.admin.tenant";
const PERMS_KEY = "fsh.admin.permissions";

export type SeededUser = {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  tenant: string;
  permissions?: string[];
};

function fakeJwt(payload: Record<string, unknown>): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  return [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");
}

export async function seedAuthedSession(page: Page, user: SeededUser) {
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
    ({ access, refresh, tenant, perms, accessKey, refreshKey, tenantKey, permsKey }) => {
      localStorage.setItem(accessKey, access);
      localStorage.setItem(refreshKey, refresh);
      localStorage.setItem(tenantKey, tenant);
      // Pre-seed permissions so RouteGuard sees them on first paint.
      // The runtime auth context still re-hydrates from /identity/permissions
      // after mount; tests should mock that endpoint to keep the in-memory
      // permissions consistent across the route's lifetime.
      localStorage.setItem(permsKey, JSON.stringify(perms));
    },
    {
      access: accessToken,
      refresh: "fake-refresh-token",
      tenant: user.tenant,
      perms: user.permissions ?? [],
      accessKey: ACCESS_KEY,
      refreshKey: REFRESH_KEY,
      tenantKey: TENANT_KEY,
      permsKey: PERMS_KEY,
    },
  );
}

export const TEST_USER: SeededUser = {
  sub: "u-test-1",
  email: "admin@root.com",
  firstName: "Root",
  lastName: "Admin",
  tenant: "root",
  permissions: [],
};
