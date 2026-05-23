import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

const LINE_ITEM = {
  id: "li-1",
  kind: "BaseFee",
  resource: null,
  description: "Pro monthly base fee",
  quantity: 1,
  unitPrice: 129.5,
  amount: 129.5,
};

const DRAFT_INVOICE = {
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
  lineItems: [LINE_ITEM],
};

const ISSUED_INVOICE = {
  ...DRAFT_INVOICE,
  id: "inv-2",
  invoiceNumber: "INV-2026-0002",
  status: "Issued",
  issuedAtUtc: "2026-05-02T00:00:00Z",
  dueAtUtc: "2026-05-16T00:00:00Z",
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("billing invoice detail", () => {
  test("loads the invoice and renders amount, status, and line items", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices/inv-1", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(DRAFT_INVOICE),
      });
    });

    await page.goto("/billing/invoices/inv-1");

    const main = page.getByRole("main");

    // Hero: invoice number + status badge + amount.
    await expect(main.getByText("INV-2026-0001", { exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText("Draft", { exact: true }).first()).toBeVisible();
    // Amount renders formatted (USD). $129.50 appears in the hero + subtotal.
    await expect(main.getByText("$129.50").first()).toBeVisible();

    // Line item description + kind badge.
    await expect(main.getByText("Pro monthly base fee", { exact: true })).toBeVisible();
    await expect(main.getByText("BaseFee", { exact: true })).toBeVisible();
  });

  test("Issue invoice POSTs to /issue for a Draft invoice", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices/inv-1", async (route) => {
      const method = route.request().method();
      if (method === "GET") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(DRAFT_INVOICE),
        });
        return;
      }
      await route.fallback();
    });
    await page.route("**/api/v1/billing/invoices/inv-1/issue", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify("inv-1"),
      });
    });

    await page.goto("/billing/invoices/inv-1");
    const main = page.getByRole("main");
    const issueBtn = main.getByRole("button", { name: /issue invoice/i });
    await expect(issueBtn).toBeEnabled({ timeout: 10_000 });

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/billing/invoices/inv-1/issue") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await issueBtn.click();
    await reqPromise;

    await expect(page.getByText(/invoice issued/i)).toBeVisible();
  });

  test("Mark as paid POSTs to /pay for an Issued invoice", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices/inv-2", async (route) => {
      const method = route.request().method();
      if (method === "GET") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(ISSUED_INVOICE),
        });
        return;
      }
      await route.fallback();
    });
    await page.route("**/api/v1/billing/invoices/inv-2/pay", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify("inv-2"),
      });
    });

    await page.goto("/billing/invoices/inv-2");
    const main = page.getByRole("main");
    const payBtn = main.getByRole("button", { name: /mark as paid/i });
    await expect(payBtn).toBeEnabled({ timeout: 10_000 });

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/billing/invoices/inv-2/pay") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await payBtn.click();
    await reqPromise;

    await expect(page.getByText(/marked paid/i)).toBeVisible();
  });

  test("Void invoice POSTs to /void with the supplied reason", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices/inv-1", async (route) => {
      const method = route.request().method();
      if (method === "GET") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(DRAFT_INVOICE),
        });
        return;
      }
      await route.fallback();
    });
    await page.route("**/api/v1/billing/invoices/inv-1/void", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify("inv-1"),
      });
    });

    await page.goto("/billing/invoices/inv-1");
    const main = page.getByRole("main");
    const voidBtn = main.getByRole("button", { name: /void invoice/i });
    await expect(voidBtn).toBeEnabled({ timeout: 10_000 });

    await main.getByLabel(/^Reason/).fill("duplicate");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/billing/invoices/inv-1/void") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await voidBtn.click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.reason).toBe("duplicate");

    await expect(page.getByText(/invoice voided/i)).toBeVisible();
  });
});
