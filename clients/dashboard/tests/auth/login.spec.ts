import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

// The dashboard login page (rebuilt to the dentalOS card layout): FSH logo
// lockup + ".NET 10 Starter Kit" caption, tenant/email/password card, and a
// demoMode-gated "Step into any role" picker that signs in instantly.

const TOKEN_RESPONSE = {
  accessToken: "header.payload.sig",
  refreshToken: "refresh",
  accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  refreshTokenExpiresAt: new Date(Date.now() + 7_200_000).toISOString(),
};

/** Force the runtime config so demoMode is deterministic per test. */
async function setConfig(page: import("@playwright/test").Page, demoMode: boolean) {
  await page.route("**/config.json", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ apiBase: "", defaultTenant: "root", demoMode }),
    }),
  );
}

test.describe("login — page chrome", () => {
  test.beforeEach(async ({ page }) => {
    await setConfig(page, true);
  });

  test("renders the FSH logo lockup with the .NET 10 caption", async ({ page }) => {
    await page.goto("/login");
    await expect(page.getByRole("img", { name: /fullstackhero/i })).toBeVisible();
    await expect(page.getByText(/\.NET 10 Starter Kit/i)).toBeVisible();
    await expect(page.getByRole("heading", { name: /welcome back/i })).toBeVisible();
    await expect(page.getByText(/sign in to your account/i)).toBeVisible();
  });

  test("renders the tenant + email + password fields", async ({ page }) => {
    await page.goto("/login");
    await expect(page.getByLabel("Tenant")).toBeVisible();
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Password", { exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: /forgot/i })).toHaveAttribute("href", "/forgot-password");
  });

  test("password visibility toggle flips the input type", async ({ page }) => {
    await page.goto("/login");
    const pwd = page.getByLabel("Password", { exact: true });
    await expect(pwd).toHaveAttribute("type", "password");
    await page.getByRole("button", { name: /show password/i }).click();
    await expect(pwd).toHaveAttribute("type", "text");
    await page.getByRole("button", { name: /hide password/i }).click();
    await expect(pwd).toHaveAttribute("type", "password");
  });

  test("submit is disabled until tenant + email + password are filled", async ({ page }) => {
    await page.goto("/login");
    const submit = page.getByRole("button", { name: /^sign in$/i });
    // Tenant defaults to "root"; fill the rest to enable.
    await expect(submit).toBeDisabled();
    await page.getByLabel("Email").fill("alice@acme.com");
    await page.getByLabel("Password", { exact: true }).fill("secret123");
    await expect(submit).toBeEnabled();
  });
});

test.describe("login — manual sign in", () => {
  test.beforeEach(async ({ page }) => {
    await setConfig(page, true);
    await mockJsonResponse(page, "**/api/v1/identity/token/issue", TOKEN_RESPONSE);
  });

  test("POSTs credentials with the tenant header", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Tenant").fill("acme");
    await page.getByLabel("Email").fill("alice@acme.com");
    await page.getByLabel("Password", { exact: true }).fill("Password123!");

    const reqPromise = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/token/issue") && r.method() === "POST",
    );
    await page.getByRole("button", { name: /^sign in$/i }).click();
    const req = await reqPromise;

    expect(req.headers().tenant).toBe("acme");
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      email: "alice@acme.com",
      password: "Password123!",
    });
  });

  test("surfaces a server error without leaving the page", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/token/issue", 401, {
      title: "Unauthorized",
      detail: "Invalid credentials.",
    });
    await page.goto("/login");
    await page.getByLabel("Email").fill("alice@acme.com");
    await page.getByLabel("Password", { exact: true }).fill("wrongpw");
    await page.getByRole("button", { name: /^sign in$/i }).click();

    await expect(page.getByRole("alert")).toContainText(/invalid credentials/i);
    await expect(page.getByRole("heading", { name: /welcome back/i })).toBeVisible();
  });
});

test.describe("login — demo account picker", () => {
  test("the demo button is hidden when demoMode is off", async ({ page }) => {
    await setConfig(page, false);
    await page.goto("/login");
    await expect(page.getByRole("button", { name: /sign in with a demo account/i })).toHaveCount(0);
  });

  test("opens the 'Step into any role' dialog and lists demo tenants", async ({ page }) => {
    await setConfig(page, true);
    await page.goto("/login");
    await page.getByRole("button", { name: /sign in with a demo account/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /step into any role/i })).toBeVisible();
    await expect(dialog.getByText(/live demo/i)).toBeVisible();
    // Tenant rail — scope to the nav so we don't collide with user rows.
    const rail = dialog.getByRole("navigation", { name: /demo tenants/i });
    await expect(rail.getByRole("button", { name: /root/i })).toBeVisible();
    await expect(rail.getByRole("button", { name: /acme corp/i })).toBeVisible();
    await expect(rail.getByRole("button", { name: /globex/i })).toBeVisible();
  });

  test("switching tenant swaps the user list", async ({ page }) => {
    await setConfig(page, true);
    await page.goto("/login");
    await page.getByRole("button", { name: /sign in with a demo account/i }).click();
    const dialog = page.getByRole("dialog");
    const rail = dialog.getByRole("navigation", { name: /demo tenants/i });

    // Root is active first → its single admin shows.
    await expect(dialog.getByText("admin@root.com")).toBeVisible();
    await rail.getByRole("button", { name: /acme corp/i }).click();
    await expect(dialog.getByText("admin@acme.com")).toBeVisible();
  });

  test("tapping a demo user signs in instantly with that account's tenant", async ({ page }) => {
    await setConfig(page, true);
    await mockJsonResponse(page, "**/api/v1/identity/token/issue", TOKEN_RESPONSE);
    await page.goto("/login");
    await page.getByRole("button", { name: /sign in with a demo account/i }).click();
    const dialog = page.getByRole("dialog");
    const rail = dialog.getByRole("navigation", { name: /demo tenants/i });

    await rail.getByRole("button", { name: /acme corp/i }).click();

    const reqPromise = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/token/issue") && r.method() === "POST",
    );
    await dialog.getByRole("button", { name: /admin@acme\.com/i }).click();
    const req = await reqPromise;

    expect(req.headers().tenant).toBe("acme");
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({ email: "admin@acme.com" });
  });
});
