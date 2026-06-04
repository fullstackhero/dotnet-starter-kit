import { expect, test, type Page } from "@playwright/test";

// Regression: a stale, EXPIRED access token left in localStorage must NOT make
// the admin app treat you as signed in. The auth provider attempts ONE silent
// refresh at boot:
//   • refresh succeeds → session is restored, no /login bounce.
//   • refresh fails    → the stale session is dropped and the app routes to
//                        /login, instead of flashing a protected surface and
//                        firing requests that 401 in a loop.
// Before the fix the app rendered as authenticated off the expired token alone
// (isAuthenticated was gated on token presence, not `exp`).

const ACCESS_KEY = "fsh.admin.accessToken";
const REFRESH_KEY = "fsh.admin.refreshToken";
const TENANT_KEY = "fsh.admin.tenant";
const PERMS_KEY = "fsh.admin.permissions";

function fakeJwt(payload: Record<string, unknown>): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  return [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");
}

const now = () => Math.floor(Date.now() / 1000);

const EXPIRED_TOKEN = fakeJwt({
  sub: "u-stale-1",
  email: "admin@root.com",
  name: "Root Admin",
  tenant: "root",
  permissions: [],
  iat: now() - 7200,
  exp: now() - 3600, // expired an hour ago
});

const FRESH_TOKEN = fakeJwt({
  sub: "u-stale-1",
  email: "admin@root.com",
  name: "Root Admin",
  tenant: "root",
  permissions: [],
  iat: now(),
  exp: now() + 3600,
});

async function seedExpiredSession(page: Page) {
  await page.route("**/config.json", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ apiBase: "", defaultTenant: "root" }),
    }),
  );
  await page.addInitScript(
    ({ access, accessKey, refreshKey, tenantKey, permsKey }) => {
      localStorage.setItem(accessKey, access);
      localStorage.setItem(refreshKey, "stale-refresh-token");
      localStorage.setItem(tenantKey, "root");
      localStorage.setItem(permsKey, "[]");
    },
    {
      access: EXPIRED_TOKEN,
      accessKey: ACCESS_KEY,
      refreshKey: REFRESH_KEY,
      tenantKey: TENANT_KEY,
      permsKey: PERMS_KEY,
    },
  );
}

test.describe("session restore — expired access token at boot", () => {
  test("routes to /login when the silent refresh fails", async ({ page }) => {
    await seedExpiredSession(page);
    await page.route("**/api/v1/identity/token/refresh", (route) =>
      route.fulfill({
        status: 401,
        headers: { "Content-Type": "application/problem+json" },
        body: JSON.stringify({ title: "Unauthorized", status: 401 }),
      }),
    );

    await page.goto("/");

    await expect(page).toHaveURL(/\/login$/);
  });

  test("silently refreshes and stays signed in when the refresh succeeds", async ({ page }) => {
    await seedExpiredSession(page);
    // Catch-all first so any post-auth data fetch (incl. the permissions
    // hydration) resolves; the refresh route is registered AFTER it so it takes
    // precedence (Playwright: last route wins).
    await page.route("**/api/v1/**", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "[]" }),
    );
    await page.route("**/api/v1/identity/token/refresh", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          token: FRESH_TOKEN,
          refreshToken: "rotated-refresh-token",
          refreshTokenExpiryTime: new Date(Date.now() + 7 * 86_400_000).toISOString(),
        }),
      }),
    );

    await page.goto("/");

    await expect
      .poll(async () => page.evaluate((k) => localStorage.getItem(k), ACCESS_KEY))
      .toBe(FRESH_TOKEN);
    await expect
      .poll(async () => page.evaluate((k) => localStorage.getItem(k), REFRESH_KEY))
      .toBe("rotated-refresh-token");
    await expect(page).not.toHaveURL(/\/login$/);
  });
});
