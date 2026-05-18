import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

test.describe("forgot-password page", () => {
  test("renders the form with the editorial eyebrow + tagline", async ({ page }) => {
    await page.goto("/forgot-password");

    // Eyebrow tag — the // 02.FORGOT-PASSWORD chrome that establishes
    // the page as part of the auth surface, not a generic form.
    await expect(page.getByText("// 02.FORGOT-PASSWORD")).toBeVisible();
    await expect(page.getByText("email · token")).toBeVisible();

    // Headline copy + form scaffolding.
    await expect(page.getByRole("heading", { name: /reset your password/i })).toBeVisible();
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Tenant")).toBeVisible();
    await expect(page.getByRole("button", { name: /send reset link/i })).toBeVisible();
  });

  test("posts to /forgot-password with the tenant header on submit", async ({ page }) => {
    const captured = captureRequest(page, "**/api/v1/identity/forgot-password");

    await page.goto("/forgot-password");
    await page.getByLabel("Email").fill("alice@acme.com");
    await page.getByLabel("Tenant").fill("acme");
    await page.getByRole("button", { name: /send reset link/i }).click();

    const { body, headers } = await captured.value();
    expect(body).toMatchObject({ email: "alice@acme.com" });
    // Tenant header must override the default for cross-tenant reset flows.
    expect(headers.tenant).toBe("acme");
  });

  test("shows the 'check your inbox' success state after 200", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/forgot-password", '""');

    await page.goto("/forgot-password");
    await page.getByLabel("Email").fill("bob@globex.com");
    await page.getByLabel("Tenant").fill("globex");
    await page.getByRole("button", { name: /send reset link/i }).click();

    // Success heading + echoed email/tenant in the confirmation copy.
    // The tenant is rendered as a separate <code> chip — target the
    // exact element to avoid the strict-mode collision with "globex"
    // inside "bob@globex.com".
    await expect(page.getByRole("heading", { name: /check your inbox/i })).toBeVisible();
    await expect(page.locator("code").filter({ hasText: /^bob@globex\.com$/ })).toBeVisible();
    await expect(page.locator("code").filter({ hasText: /^globex$/ })).toBeVisible();

    // "Try a different address" affordance lets the user retry without
    // a page reload — important when they typo their email.
    await expect(page.getByRole("button", { name: /try a different address/i })).toBeVisible();
  });

  test("surfaces server errors (5xx / tenant-not-resolvable) on the form", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/forgot-password", 500, {
      title: "Tenant resolution failed",
      detail: "No tenant matching the supplied identifier.",
    });

    await page.goto("/forgot-password");
    await page.getByLabel("Email").fill("alice@acme.com");
    await page.getByLabel("Tenant").fill("does-not-exist");
    await page.getByRole("button", { name: /send reset link/i }).click();

    // Error band is rendered (role=alert) with the server's detail surfaced.
    const alert = page.getByRole("alert");
    await expect(alert).toBeVisible();
    await expect(alert).toContainText(/no tenant matching/i);

    // Still on the form, not the success state.
    await expect(page.getByRole("heading", { name: /check your inbox/i })).not.toBeVisible();
  });
});
