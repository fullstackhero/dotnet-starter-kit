import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

const TENANT_ACME = {
  id: "acme",
  name: "Acme Corp",
  adminEmail: "admin@acme.com",
  isActive: true,
  validUpto: "2027-01-01T00:00:00Z",
  issuer: "fsh.demo.acme",
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("tenants registry list", () => {
  test("renders the Registry heading and a tenant row from the mock", async ({ page }) => {
    await page.route("**/api/v1/tenants/?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([TENANT_ACME])),
      });
    });

    await page.goto("/tenants");

    await expect(
      page.getByRole("heading", { name: "Registry", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // The tenant row from our mock. The name renders in both a (hidden) mobile
    // card and the desktop row, so scope to the desktop row button — its
    // accessible name carries the tenant name + id. The admin email also appears
    // in both variants (the mobile one is display:none on desktop), so scope it
    // to the desktop row button to assert the visible occurrence.
    const desktopRow = page.getByRole("button", { name: /Acme Corp/ });
    await expect(desktopRow).toBeVisible();
    await expect(desktopRow.getByText("admin@acme.com", { exact: true })).toBeVisible();
  });

  test("shows the empty state when no tenants are registered", async ({ page }) => {
    await page.route("**/api/v1/tenants/?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([])),
      });
    });

    await page.goto("/tenants");

    await expect(page.getByText("No tenants yet.", { exact: true })).toBeVisible({
      timeout: 10_000,
    });
    await expect(
      page.getByText("Provision the first tenant to get started.", { exact: true }),
    ).toBeVisible();
  });

  test("the New tenant button opens the create dialog", async ({ page }) => {
    await page.route("**/api/v1/tenants/?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([TENANT_ACME])),
      });
    });

    await page.goto("/tenants");
    await expect(
      page.getByRole("heading", { name: "Registry", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Creation is now an in-page dialog, not a /tenants/new route. Clicking the
    // trigger opens the Radix dialog rather than navigating. `exact` keeps the
    // trigger off the dialog's own "Create tenant" submit button.
    await page.getByRole("button", { name: "New tenant", exact: true }).click();

    await expect(page).toHaveURL(/\/tenants$/);
    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: "New tenant", exact: true }),
    ).toBeVisible();
  });
});
