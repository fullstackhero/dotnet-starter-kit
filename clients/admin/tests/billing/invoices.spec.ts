import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

// An InvoiceDto matching src/api/billing.ts.
const INVOICE_DRAFT = {
  id: "inv-1",
  tenantId: "acme",
  invoiceNumber: "INV-2026-0001",
  periodYear: 2026,
  periodMonth: 5,
  currency: "USD",
  subtotalAmount: 129.5,
  status: "Draft",
  createdAtUtc: "2026-05-01T00:00:00Z",
  issuedAtUtc: null,
  dueAtUtc: null,
  paidAtUtc: null,
  voidedAtUtc: null,
  notes: null,
  lineItems: [],
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("billing invoices list", () => {
  test("renders the Invoices heading, an invoice row, and its status badge", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices?*", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([INVOICE_DRAFT])),
      });
    });

    await page.goto("/billing/invoices");

    const main = page.getByRole("main");

    // CardTitle "Invoices" — scope to main and exclude the BillingLayout tab
    // link (also "Invoices", rendered as an <a>). The CardTitle is a <div>.
    await expect(
      main.locator("div", { hasText: /^Invoices$/ }).first(),
    ).toBeVisible({ timeout: 10_000 });

    // The invoice row from our mock: number code + status badge. Target the
    // visible badge <span>; the status filter is a closed dropdown, so its
    // "Draft" item isn't in the DOM to collide with.
    await expect(main.getByText("INV-2026-0001", { exact: true })).toBeVisible();
    await expect(main.locator("span", { hasText: /^Draft$/ })).toBeVisible();
  });

  test("shows the empty state when no invoices match", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices?*", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([])),
      });
    });

    await page.goto("/billing/invoices");

    await expect(
      page.getByText(/no invoices match the current filters/i),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("clicking an invoice row navigates to the detail page", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices?*", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([INVOICE_DRAFT])),
      });
    });
    // The detail page fetches the single invoice on navigation.
    await page.route("**/api/v1/billing/invoices/inv-1", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(INVOICE_DRAFT),
      });
    });

    await page.goto("/billing/invoices");
    const main = page.getByRole("main");
    await expect(main.getByText("INV-2026-0001", { exact: true })).toBeVisible({ timeout: 10_000 });

    await main.getByRole("button", { name: /INV-2026-0001/ }).click();

    await expect(page).toHaveURL(/\/billing\/invoices\/inv-1$/);
  });
});
