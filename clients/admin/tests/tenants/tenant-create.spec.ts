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
  // The dialog loads active billing plans on open (to populate the plan picker
  // and preselect the trial plan). Mock it so the query resolves cleanly — an
  // unmocked call would 401 against a real dev backend and log the session out
  // mid-test.
  await page.route("**/api/v1/billing/plans**", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify([
        {
          id: "plan-free",
          key: "free",
          name: "Free",
          currency: "USD",
          monthlyBasePrice: 0,
          overageRates: {},
          isActive: true,
          interval: "Monthly",
          annualPrice: null,
        },
      ]),
    }),
  );
});

test.describe("tenant create dialog", () => {
  test("renders the core fields and tucks issuer/database behind Advanced", async ({ page }) => {
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

    await expect(dialog.getByLabel(/^Display name/)).toBeVisible();
    await expect(dialog.getByLabel(/^Identifier/)).toBeVisible();
    await expect(dialog.getByLabel(/^Admin email/)).toBeVisible();
    await expect(dialog.getByLabel(/^Initial admin password/)).toBeVisible();

    // JWT issuer + connection string now live under a collapsed "Advanced" section.
    await expect(dialog.getByLabel(/^JWT issuer/)).toBeHidden();
    await dialog.getByRole("button", { name: /^Advanced/ }).click();
    await expect(dialog.getByLabel(/^JWT issuer/)).toBeVisible();
    await expect(dialog.getByLabel(/^Connection string/)).toBeVisible();

    await expect(
      dialog.getByRole("button", { name: "Create tenant", exact: true }),
    ).toBeVisible();
  });

  test("auto-derives the identifier (and issuer) from the display name", async ({ page }) => {
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
          issuer: "acme-corp",
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
    await expect(dialog.getByLabel(/^Display name/)).toBeVisible({ timeout: 10_000 });

    // Typing the display name fills the identifier automatically — the operator
    // never has to type the slug by hand.
    await dialog.getByLabel(/^Display name/).fill("Acme Corp");
    await expect(dialog.getByLabel(/^Identifier/)).toHaveValue("acme-corp");

    await dialog.getByLabel(/^Admin email/).fill("admin@acme.example");
    await dialog.getByLabel(/^Initial admin password/).fill("Sup3rSecret!");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/tenants/") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: "Create tenant", exact: true }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    // Identifier and issuer are both derived from the display name.
    expect(body).toMatchObject({
      id: "acme-corp",
      name: "Acme Corp",
      adminEmail: "admin@acme.example",
      adminPassword: "Sup3rSecret!",
      issuer: "acme-corp",
    });
  });

  test("unlocking the identifier lets you type a custom slug", async ({ page }) => {
    await page.goto("/tenants");
    await page
      .getByRole("button", { name: "New tenant", exact: true })
      .click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^Display name/)).toBeVisible({ timeout: 10_000 });

    await dialog.getByLabel(/^Display name/).fill("Acme Corp");
    await expect(dialog.getByLabel(/^Identifier/)).toHaveValue("acme-corp");

    // Auto-derived identifier is read-only until unlocked.
    await expect(dialog.getByLabel(/^Identifier/)).toHaveAttribute("readonly", "");
    await dialog.getByRole("button", { name: "Edit" }).click();
    await dialog.getByLabel(/^Identifier/).fill("acme-emea");
    await expect(dialog.getByLabel(/^Identifier/)).toHaveValue("acme-emea");

    // Re-locking snaps it back to the slug derived from the display name.
    await dialog.getByRole("button", { name: "Auto" }).click();
    await expect(dialog.getByLabel(/^Identifier/)).toHaveValue("acme-corp");
  });

  test("the password generator fills a strong value and reveals it", async ({ page }) => {
    await page.goto("/tenants");
    await page
      .getByRole("button", { name: "New tenant", exact: true })
      .click();

    const dialog = page.getByRole("dialog");
    const password = dialog.getByLabel(/^Initial admin password/);
    await expect(password).toBeVisible({ timeout: 10_000 });

    await expect(password).toHaveAttribute("type", "password");
    await dialog.getByRole("button", { name: "Generate strong password" }).click();

    // Generating reveals the field and writes a non-trivial secret.
    await expect(password).toHaveAttribute("type", "text");
    const value = await password.inputValue();
    expect(value.length).toBeGreaterThanOrEqual(12);
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
    await expect(dialog.getByLabel(/^Display name/)).toBeVisible({ timeout: 10_000 });

    // Drive an invalid identifier by unlocking the field and typing a bad slug.
    // We leave the email blank so the browser's native <input type=email>
    // constraint doesn't pre-empt react-hook-form's submit handler — that lets
    // zod report the field-level errors we assert on.
    await dialog.getByLabel(/^Display name/).fill("Acme Corp");
    await dialog.getByRole("button", { name: "Edit" }).click();
    await dialog.getByLabel(/^Identifier/).fill("A");
    await dialog.getByLabel(/^Initial admin password/).fill("short");

    await dialog.getByRole("button", { name: "Create tenant", exact: true }).click();

    // Zod messages surface and no POST is fired.
    await expect(
      dialog.getByText(/Lowercase letters, digits, hyphens/i),
    ).toBeVisible();
    await expect(dialog.getByText(/At least 8 characters\./i)).toBeVisible();
    expect(posted).toBe(false);
  });
});
