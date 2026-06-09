// E2E coverage for the impersonation-revoked terminal flow.
//
// When an operator's impersonation grant is revoked (or its short-lived token
// expires) mid-session, the server starts rejecting the impersonation token
// with a 401. Impersonation sessions carry no refresh token, so that 401
// propagates straight through apiFetch; a global query/mutation error hook
// (query-client.ts → isImpersonationRevokedError) routes to the dedicated
// /impersonation-ended page instead of leaving a dead error banner under a
// half-loaded dashboard.
//
// Browser: chromium only, run against the already-running Vite dev server.

import { expect, test, type Page } from "@playwright/test";
import { mockProblemDetails } from "../helpers/api-mocks";
import { installShellMocks } from "../helpers/shell-mocks";

const ACCESS_KEY = "fsh.dashboard.accessToken";
const REFRESH_KEY = "fsh.dashboard.refreshToken";
const TENANT_KEY = "fsh.dashboard.tenant";

/**
 * Seed an IMPERSONATION session into localStorage before React boots: an
 * access token carrying the `act_sub` actor claim, a target tenant, and —
 * critically — NO refresh token (token-store drops the refresh slot on
 * beginImpersonation). The missing refresh token is what makes a 401 propagate
 * to the global error hook rather than triggering a silent refresh-and-retry.
 */
async function seedImpersonationSession(page: Page): Promise<void> {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  const payload = {
    sub: "u-impersonated-1",
    email: "dan@acme.com",
    name: "Dan Mueller",
    tenant: "acme",
    // Actor claims — the original operator's identity. Their presence is what
    // marks this token as an impersonation session.
    act_sub: "op-root-1",
    act_tenant: "root",
    act_name: "Root Operator",
    permissions: [],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  };
  const accessToken = [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");

  await page.addInitScript(
    ({ access, accessKey, refreshKey, tenantKey }) => {
      localStorage.setItem(accessKey, access);
      // Defensive: ensure no refresh token lingers from a prior session.
      localStorage.removeItem(refreshKey);
      localStorage.setItem(tenantKey, "acme");
    },
    { access: accessToken, accessKey: ACCESS_KEY, refreshKey: REFRESH_KEY, tenantKey: TENANT_KEY },
  );
}

test.beforeEach(async ({ page }) => {
  await seedImpersonationSession(page);
  await installShellMocks(page);
});

test.describe("impersonation revoked mid-session", () => {
  test("a 401 on an impersonation session routes to the terminal page", async ({ page }) => {
    // The products list 401s the moment the grant is revoked. The dev build
    // surfaces the JwtBearer rejection reason on the ProblemDetails.
    await mockProblemDetails(page, "**/api/v1/catalog/products**", 401, {
      title: "Unauthorized",
      detail: "Authentication is required to access this resource.",
    });

    await page.goto("/catalog/products");

    // Lands on the dedicated terminal page rather than showing an inline
    // error band under the half-loaded catalog.
    await expect(page).toHaveURL(/\/impersonation-ended$/);
    await expect(
      page.getByRole("heading", { name: /impersonation ended/i }),
    ).toBeVisible();
    await expect(
      page.getByText(/revoked or has expired/i),
    ).toBeVisible();
  });

  test("'Back to sign in' clears the dead token and routes to /login", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/catalog/products**", 401, {
      title: "Unauthorized",
      detail: "Authentication is required to access this resource.",
    });

    await page.goto("/catalog/products");
    await expect(page).toHaveURL(/\/impersonation-ended$/);

    await page.getByRole("button", { name: /back to sign in/i }).click();

    await expect(page).toHaveURL(/\/login$/);
    // The dead impersonation token is cleared so a stale token can't bounce
    // the user straight back into a 401 loop.
    const accessToken = await page.evaluate((key) => localStorage.getItem(key), ACCESS_KEY);
    expect(accessToken).toBeNull();
  });
});
