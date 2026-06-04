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

  test("hides Issue/Mark-paid/Void + Download for a Billing.View-only user", async ({ page }) => {
    // Keep Billing.View so the route guard passes; drop Billing.Manage.
    const viewOnly = ADMIN_PERMS.filter((p) => p !== "Permissions.Billing.Manage");
    await seedAuthedSession(page, { ...TEST_USER, permissions: viewOnly });
    await installAdminShellMocks(page, viewOnly);

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

    // Read-only content still renders.
    await expect(main.getByText("INV-2026-0001", { exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText("Pro monthly base fee", { exact: true })).toBeVisible();

    // Every manage affordance must be absent.
    await expect(main.getByRole("button", { name: /issue invoice/i })).toHaveCount(0);
    await expect(main.getByRole("button", { name: /mark as paid/i })).toHaveCount(0);
    await expect(main.getByRole("button", { name: /void invoice/i })).toHaveCount(0);
    await expect(main.getByRole("button", { name: /download pdf/i })).toHaveCount(0);
  });

  test("renders an error state (not a stuck Loading…) when the invoice fails to load", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices/inv-err", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 500,
        headers: { "Content-Type": "application/problem+json" },
        body: JSON.stringify({ title: "Server error", detail: "Boom." }),
      });
    });

    await page.goto("/billing/invoices/inv-err");
    const main = page.getByRole("main");

    // Line-items section surfaces the error and does NOT stick on "Loading…".
    await expect(main.getByText(/boom\.|failed to load/i).first()).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText("Loading…", { exact: true })).toHaveCount(0);
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

  test("Download PDF fetches the invoice /pdf endpoint", async ({ page }) => {
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
    // Stub the PDF stream with a tiny fake body so the blob download succeeds.
    await page.route("**/api/v1/billing/invoices/inv-1/pdf", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/pdf" },
        body: "%PDF-1.4 fake",
      });
    });

    await page.goto("/billing/invoices/inv-1");
    const main = page.getByRole("main");
    const downloadBtn = main.getByRole("button", { name: /download pdf/i });
    await expect(downloadBtn).toBeEnabled({ timeout: 10_000 });

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/billing/invoices/inv-1/pdf") && r.method() === "GET",
      { timeout: 5_000 },
    );
    await downloadBtn.click();
    const req = await reqPromise;

    // Replicates getInvoice's auth + tenant headers so cross-tenant viewing works.
    expect(req.headers()["authorization"]).toMatch(/^Bearer /);
    expect(req.headers()["tenant"]).toBe("root");
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
