import { expect, test, type Page } from "@playwright/test";

// Regression: a stale, EXPIRED access token left in localStorage (e.g. from a
// session days ago, or before a DB reseed) must NOT make the app treat you as
// signed in. The auth provider attempts ONE silent refresh at boot:
//   • refresh succeeds → session is restored, no /login bounce.
//   • refresh fails    → the stale session is dropped and the app routes to
//                        /login, instead of flashing the dashboard and firing
//                        protected requests that 401 in a loop.
// Before the fix the dashboard rendered as authenticated off the expired token
// alone (isAuthenticated was gated on token presence, not `exp`).

const ACCESS_KEY = "fsh.dashboard.accessToken";
const REFRESH_KEY = "fsh.dashboard.refreshToken";
const TENANT_KEY = "fsh.dashboard.tenant";

function fakeJwt(payload: Record<string, unknown>): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  return [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");
}

const now = () => Math.floor(Date.now() / 1000);

const EXPIRED_TOKEN = fakeJwt({
  sub: "u-stale-1",
  email: "alice@acme.com",
  name: "Alice Nguyen",
  tenant: "acme",
  permissions: [],
  iat: now() - 7200,
  exp: now() - 3600, // expired an hour ago
});

const FRESH_TOKEN = fakeJwt({
  sub: "u-stale-1",
  email: "alice@acme.com",
  name: "Alice Nguyen",
  tenant: "acme",
  permissions: [],
  iat: now(),
  exp: now() + 3600,
});

async function seedExpiredSession(page: Page) {
  await page.route("**/config.json", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ apiBase: "", defaultTenant: "acme", demoMode: true }),
    }),
  );
  await page.addInitScript(
    ({ access, accessKey, refreshKey, tenantKey }) => {
      localStorage.setItem(accessKey, access);
      localStorage.setItem(refreshKey, "stale-refresh-token");
      localStorage.setItem(tenantKey, "acme");
    },
    { access: EXPIRED_TOKEN, accessKey: ACCESS_KEY, refreshKey: REFRESH_KEY, tenantKey: TENANT_KEY },
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
    await expect(page.getByRole("heading", { name: /welcome back/i })).toBeVisible();
  });

  test("silently refreshes and stays signed in when the refresh succeeds", async ({ page }) => {
    await seedExpiredSession(page);
    // Catch-all first so any post-auth data fetch resolves; the refresh route is
    // registered AFTER it so it takes precedence (Playwright: last route wins).
    await page.route("**/api/v1/**", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "{}" }),
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

    // The silent refresh persisted the rotated tokens…
    await expect
      .poll(async () => page.evaluate((k) => localStorage.getItem(k), ACCESS_KEY))
      .toBe(FRESH_TOKEN);
    await expect
      .poll(async () => page.evaluate((k) => localStorage.getItem(k), REFRESH_KEY))
      .toBe("rotated-refresh-token");
    // …and the app did NOT bounce to /login.
    await expect(page).not.toHaveURL(/\/login$/);
  });
});
