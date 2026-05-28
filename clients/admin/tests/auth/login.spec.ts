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
  test("renders the FSH brand lockup + the welcome form", async ({ page }) => {
    await page.goto("/login");

    // Brand lockup: logo image, the fullstackhero wordmark, and the
    // "Platform Admin" divider label that marks this as the operator app.
    await expect(page.getByRole("img", { name: /fullstackhero/i }).first()).toBeVisible();
    await expect(page.getByText("fullstackhero").first()).toBeVisible();
    await expect(page.getByText("Platform Admin").first()).toBeVisible();

    // Card heading.
    await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();

    // Form fields (plain labels). Password is exact — a "Show password"
    // toggle button shares the substring otherwise.
    await expect(page.getByLabel("Tenant")).toBeVisible();
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Password", { exact: true })).toBeVisible();

    // Submit (exact — the dev demo button also contains "Sign in") + forgot link.
    await expect(page.getByRole("button", { name: "Sign in", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "Forgot?" })).toBeVisible();
  });

  test("manual sign-in posts to token/issue with the admin app + tenant headers", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/token/issue", TOKEN_RESPONSE);

    await page.goto("/login");

    // Tenant pre-fills from env.defaultTenant ("root"); set email + password.
    await page.getByLabel("Tenant").fill("root");
    await page.getByLabel("Email").fill("operator@root.example");
    await page.getByLabel("Password", { exact: true }).fill("Sup3rSecret!");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/token/issue") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: "Sign in", exact: true }).click();
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
    await page.getByLabel("Password", { exact: true }).fill("wrong-password");
    await page.getByRole("button", { name: "Sign in", exact: true }).click();

    await expect(page.getByText("Invalid credentials.")).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test("DEV demo dialog lists the superadmin account and signs in on pick", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/token/issue", TOKEN_RESPONSE);
    await page.goto("/login");

    // The dev server runs in DEV, so the demo affordance is rendered. It now
    // opens a dialog account picker (the old inline "callout" was replaced).
    await page.getByRole("button", { name: "Sign in with a demo account" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText("superadmin@root.com")).toBeVisible();

    // Picking the account fills the creds and signs in instantly — assert the
    // resulting token/issue POST carries the demo email + tenant header.
    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/token/issue") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: /SuperAdmin/ }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.email).toBe("superadmin@root.com");
    expect(req.headers().tenant).toBe("root");
  });
});
