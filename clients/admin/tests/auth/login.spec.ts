import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

// LoginPage is the PUBLIC root-operator sign-in. We deliberately do NOT seed
// an authed session here — a seeded session makes <LoginPage> redirect away
// (Navigate to "/") and the form never renders.
//
// The login form posts to /api/v1/identity/token/issue with the `tenant`
// header + `X-FSH-App: admin`, and a body of { email, password } (the tenant
// rides the header, not the body). See src/auth/api.ts.

const TOKEN_RESPONSE = {
  accessToken: "fake.access.token",
  refreshToken: "fake-refresh-token",
  accessTokenExpiresAt: "2099-01-01T00:00:00Z",
  refreshTokenExpiresAt: "2099-01-08T00:00:00Z",
};

test.describe("admin login", () => {
  test("renders the FSH brand lockup + the // AUTHENTICATE form", async ({ page }) => {
    await page.goto("/login");

    // Brand lockup (BrandMarkXL). The wordmark + monogram appear in both the
    // left brand pane and the mobile-only header, so scope to the form column
    // for the wordmark/monogram and use .first() for the logo image.
    await expect(page.getByRole("img", { name: /fullstackhero/i }).first()).toBeVisible();
    await expect(page.getByText("fullstackhero").first()).toBeVisible();
    await expect(page.getByText("Console.", { exact: true }).first()).toBeVisible();

    // Section rule crumb.
    await expect(page.getByText("// AUTHENTICATE")).toBeVisible();

    // Form fields (plain labels, exact match is safe).
    await expect(page.getByLabel("Tenant")).toBeVisible();
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Password")).toBeVisible();

    // Submit + forgot-password link.
    await expect(page.getByRole("button", { name: "Sign in →" })).toBeVisible();
    await expect(page.getByRole("link", { name: "// forgot password?" })).toBeVisible();
  });

  test("manual sign-in posts to token/issue with the admin app + tenant headers", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/token/issue", TOKEN_RESPONSE);

    await page.goto("/login");

    // Tenant pre-fills from env.defaultTenant ("root"); set email + password.
    await page.getByLabel("Tenant").fill("root");
    await page.getByLabel("Email").fill("operator@root.example");
    await page.getByLabel("Password").fill("Sup3rSecret!");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/token/issue") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: "Sign in →" }).click();
    const req = await reqPromise;

    // X-FSH-App marks this as the platform-admin client; tenant rides the header.
    expect(req.headers()["x-fsh-app"]).toBe("admin");
    expect(req.headers().tenant).toBe("root");

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.email).toBe("operator@root.example");
    expect(body.password).toBe("Sup3rSecret!");
  });

  test("surfaces a 401 from token/issue and stays on /login", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/token/issue", 401, {
      title: "Unauthorized",
      detail: "Invalid credentials.",
    });

    await page.goto("/login");
    await page.getByLabel("Tenant").fill("root");
    await page.getByLabel("Email").fill("operator@root.example");
    await page.getByLabel("Password").fill("wrong-password");
    await page.getByRole("button", { name: "Sign in →" }).click();

    await expect(page.getByText("Invalid credentials.")).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test("DEV demo callout renders and 'use →' prefills the email field", async ({ page }) => {
    await page.goto("/login");

    // The dev server runs in DEV, so the demo callout region is rendered.
    const callout = page.getByRole("region", { name: /development demo account/i });
    await expect(callout).toBeVisible();
    await expect(callout.getByText("superadmin@root.com")).toBeVisible();

    // Clicking the account button ("use →") prefills the email field.
    await callout.getByRole("button", { name: /use →/i }).click();
    await expect(page.getByLabel("Email")).toHaveValue("superadmin@root.com");
  });
});
