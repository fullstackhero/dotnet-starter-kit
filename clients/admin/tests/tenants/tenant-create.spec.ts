import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  // Tenant creation is now a dialog launched from the list page, so every test
  // lands on /tenants first — keep the list query satisfied so the page (and
  // its "New tenant" trigger) renders.
  await page.route("**/api/v1/tenants/?*", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(paged([])),
    }),
  );
});

test.describe("tenant create dialog", () => {
  test("renders all required fields and the create action", async ({ page }) => {
    await page.goto("/tenants");

    // Open the create dialog from the list-page trigger. `exact` keeps this off
    // the dialog's own "Create tenant" submit button.
    await page
      .getByRole("button", { name: "New tenant", exact: true })
      .click();

    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: "New tenant", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(dialog.getByLabel(/^Identifier/)).toBeVisible();
    await expect(dialog.getByLabel(/^Display name/)).toBeVisible();
    await expect(dialog.getByLabel(/^Admin email/)).toBeVisible();
    await expect(dialog.getByLabel(/^Initial admin password/)).toBeVisible();
    await expect(dialog.getByLabel(/^JWT issuer/)).toBeVisible();

    await expect(
      dialog.getByRole("button", { name: "Create tenant", exact: true }),
    ).toBeVisible();
  });

  test("filling the form and submitting POSTs to /api/v1/tenants/", async ({ page }) => {
    await page.route("**/api/v1/tenants/", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: "acme-corp", status: "Queued" }),
      });
    });
    // The create handler navigates to the detail page on success; mock its loads.
    await page.route("**/api/v1/tenants/*/status", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "acme-corp",
          name: "Acme Corp",
          adminEmail: "admin@acme.example",
          isActive: true,
          validUpto: "2027-01-01T00:00:00Z",
          issuer: "acme-corp.issuer",
        }),
      }),
    );
    await page.route("**/api/v1/tenants/*/provisioning", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: "Running", steps: [], correlationId: "x" }),
      }),
    );
    await page.route("**/api/v1/tenants/theme", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "{}" }),
    );
    await page.route("**/api/v1/identity/impersonation/grants**", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "[]" }),
    );

    await page.goto("/tenants");
    await page
      .getByRole("button", { name: "New tenant", exact: true })
      .click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^Identifier/)).toBeVisible({ timeout: 10_000 });

    await dialog.getByLabel(/^Identifier/).fill("acme-corp");
    await dialog.getByLabel(/^Display name/).fill("Acme Corp");
    await dialog.getByLabel(/^Admin email/).fill("admin@acme.example");
    await dialog.getByLabel(/^Initial admin password/).fill("Sup3rSecret!");
    await dialog.getByLabel(/^JWT issuer/).fill("acme-corp.issuer");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/tenants/") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: "Create tenant", exact: true }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body).toMatchObject({
      id: "acme-corp",
      name: "Acme Corp",
      adminEmail: "admin@acme.example",
      adminPassword: "Sup3rSecret!",
      issuer: "acme-corp.issuer",
    });
  });

  test("client-side validation blocks submit on an invalid identifier", async ({ page }) => {
    let posted = false;
    await page.route("**/api/v1/tenants/", async (route) => {
      if (route.request().method() === "POST") posted = true;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: "x" }),
      });
    });

    await page.goto("/tenants");
    await page
      .getByRole("button", { name: "New tenant", exact: true })
      .click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^Identifier/)).toBeVisible({ timeout: 10_000 });

    // Invalid identifier (uppercase + too short) and a too-short password.
    // We leave the email blank so the browser's native <input type=email>
    // constraint doesn't pre-empt react-hook-form's submit handler — that lets
    // zod report the field-level errors we assert on.
    await dialog.getByLabel(/^Identifier/).fill("A");
    await dialog.getByLabel(/^Display name/).fill("Acme Corp");
    await dialog.getByLabel(/^Initial admin password/).fill("short");
    await dialog.getByLabel(/^JWT issuer/).fill("acme-corp.issuer");

    await dialog.getByRole("button", { name: "Create tenant", exact: true }).click();

    // Zod messages surface and no POST is fired.
    await expect(
      dialog.getByText(/Lowercase letters, digits, hyphens/i),
    ).toBeVisible();
    await expect(dialog.getByText(/At least 8 characters\./i)).toBeVisible();
    expect(posted).toBe(false);
  });
});
