import { expect, test, type Page, type Route } from "@playwright/test";
import { fakeJwt } from "../helpers/auth-seed";
import { installAdminShellMocks } from "../helpers/shell-mocks";

// Two-step sign-in (#1446). When the account has 2FA on, POST /token/issue
// without a code answers 401 with ProblemDetails `code: Identity.TwoFactorRequired`;
// the login card swaps to a code step and repeats the call with `twoFactorCode`.
// A wrong code answers `code: Identity.TwoFactorInvalid`.

const VALID_CODE = "123456";
const EMAIL = "operator@root.example";
const PASSWORD = "Password123!";

function tokenResponse() {
  const now = Math.floor(Date.now() / 1000);
  return {
    accessToken: fakeJwt({
      sub: "u-2fa",
      email: EMAIL,
      given_name: "Root",
      family_name: "Operator",
      tenant: "root",
      exp: now + 3600,
      iat: now,
    }),
    refreshToken: "refresh",
    accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
    refreshTokenExpiresAt: new Date(Date.now() + 7_200_000).toISOString(),
  };
}

function problem(route: Route, code: string, detail: string) {
  return route.fulfill({
    status: 401,
    headers: { "Content-Type": "application/problem+json" },
    body: JSON.stringify({
      type: "https://httpstatuses.io/401",
      title: "Unauthorized",
      status: 401,
      detail,
      code,
    }),
  });
}

type IssueBody = { email: string; password: string; twoFactorCode?: string };

/**
 * Mock the token endpoint as the API behaves for a user with (or without)
 * 2FA enabled, recording every body the page posts.
 */
async function mockTokenIssue(page: Page, opts: { twoFactorEnabled: boolean }) {
  const bodies: IssueBody[] = [];
  const tenants: (string | undefined)[] = [];
  await page.route("**/api/v1/identity/token/issue", async (route) => {
    const body = JSON.parse(route.request().postData() ?? "{}") as IssueBody;
    bodies.push(body);
    tenants.push(route.request().headers().tenant);
    if (!opts.twoFactorEnabled) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(tokenResponse()) });
      return;
    }
    if (!body.twoFactorCode) {
      await problem(route, "Identity.TwoFactorRequired", "two_factor_required: An authenticator code is required to complete sign-in.");
      return;
    }
    if (body.twoFactorCode !== VALID_CODE) {
      await problem(route, "Identity.TwoFactorInvalid", "two_factor_invalid: The authenticator code is invalid or expired.");
      return;
    }
    await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(tokenResponse()) });
  });
  return Object.assign(bodies, { tenants });
}

test.beforeEach(async ({ page }) => {
  // After sign-in the operator shell boots on "/". Answer anything it fetches with
  // an empty 200 so no stray 401 logs the fresh session out mid-test.
  await page.route("**/api/v1/**", (route) =>
    route.fulfill({ status: 200, contentType: "application/json", body: "[]" }),
  );
  await installAdminShellMocks(page);
});

async function submitPassword(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Tenant").fill("root");
  await page.getByLabel("Email").fill(EMAIL);
  await page.getByLabel("Password", { exact: true }).fill(PASSWORD);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
}

test.describe("login — two-factor step", () => {
  test("asks for the code, then signs in with it", async ({ page }) => {
    const bodies = await mockTokenIssue(page, { twoFactorEnabled: true });
    await submitPassword(page);

    await expect(page.getByRole("heading", { name: /two-step verification/i })).toBeVisible();
    const codeInput = page.getByLabel("Authentication code");
    await expect(codeInput).toBeFocused();
    await expect(codeInput).toHaveAttribute("autocomplete", "one-time-code");
    await expect(codeInput).toHaveAttribute("inputmode", "numeric");
    // No error surfaces for the expected 2FA challenge.
    await expect(page.getByRole("alert")).toHaveCount(0);

    const verify = page.getByRole("button", { name: /^verify$/i });
    await expect(verify).toBeDisabled();
    await codeInput.fill(VALID_CODE);
    await verify.click();

    await expect(page).not.toHaveURL(/\/login/);
    expect(bodies).toHaveLength(2);
    expect(bodies[0]).toEqual({ email: EMAIL, password: PASSWORD });
    expect(bodies[1]).toEqual({ email: EMAIL, password: PASSWORD, twoFactorCode: VALID_CODE });
    // The tenant chosen on the password step rides the retry too.
    expect(bodies.tenants).toEqual(["root", "root"]);

    // The password never lands in browser storage.
    const stored = await page.evaluate(() => JSON.stringify({ ...localStorage, ...sessionStorage }));
    expect(stored).not.toContain(PASSWORD);
  });

  test("a wrong code shows an error and stays on the code step", async ({ page }) => {
    const bodies = await mockTokenIssue(page, { twoFactorEnabled: true });
    await submitPassword(page);

    const codeInput = page.getByLabel("Authentication code");
    await codeInput.fill("000000");
    await page.getByRole("button", { name: /^verify$/i }).click();

    await expect(page.getByRole("alert")).toContainText(/that code didn't work/i);
    await expect(page.getByRole("heading", { name: /two-step verification/i })).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
    await expect(codeInput).toHaveValue("");
    expect(bodies[1]).toMatchObject({ twoFactorCode: "000000" });

    // A second attempt with the right code still works from here.
    await codeInput.fill(VALID_CODE);
    await page.getByRole("button", { name: /^verify$/i }).click();
    await expect(page).not.toHaveURL(/\/login/);
  });

  test("back returns to the password step with the password cleared", async ({ page }) => {
    await mockTokenIssue(page, { twoFactorEnabled: true });
    await submitPassword(page);

    await expect(page.getByLabel("Authentication code")).toBeVisible();
    await page.getByRole("button", { name: /back to sign in/i }).click();

    await expect(page.getByRole("heading", { name: /welcome back/i })).toBeVisible();
    await expect(page.getByLabel("Email")).toHaveValue(EMAIL);
    await expect(page.getByLabel("Password", { exact: true })).toHaveValue("");
    await expect(page.getByLabel("Authentication code")).toHaveCount(0);
  });

  test("a user without 2FA still signs in in one step", async ({ page }) => {
    const bodies = await mockTokenIssue(page, { twoFactorEnabled: false });
    await submitPassword(page);

    await expect(page).not.toHaveURL(/\/login/);
    expect(bodies).toHaveLength(1);
    expect(bodies[0]).toEqual({ email: EMAIL, password: PASSWORD });
  });
});
