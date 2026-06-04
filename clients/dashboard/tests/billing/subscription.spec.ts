import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

// ── Fixtures ─────────────────────────────────────────────────────────

const now = new Date();
const PERIOD_YEAR = now.getUTCFullYear();
const PERIOD_MONTH = now.getUTCMonth() + 1;

const SUBSCRIPTION = {
  id: "sub-1",
  tenantId: "acme",
  planId: "plan-scale",
  planKey: "Scale",
  startUtc: new Date(Date.UTC(2026, 0, 1)).toISOString(),
  endUtc: null,
  status: "Active",
};

/** Status with plenty of runway — banner stays hidden. */
const HEALTHY_STATUS = {
  id: "acme",
  name: "Acme Corp",
  isActive: true,
  validUpto: new Date(Date.now() + 90 * 24 * 60 * 60 * 1000).toISOString(),
  hasConnectionString: false,
  adminEmail: "admin@acme.com",
  issuer: null,
  plan: "Scale",
  expiryState: "Active",
  graceEndsUtc: new Date(Date.now() + 97 * 24 * 60 * 60 * 1000).toISOString(),
};

/** Active but within the 7-day window — soft info bar should appear. */
const NEARING_STATUS = {
  ...HEALTHY_STATUS,
  validUpto: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString(),
  expiryState: "Active",
};

/** Expired, still inside the grace window — warning bar should appear. */
const GRACE_STATUS = {
  ...HEALTHY_STATUS,
  validUpto: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
  expiryState: "InGrace",
  graceEndsUtc: new Date(Date.now() + 5 * 24 * 60 * 60 * 1000).toISOString(),
};

/** Fully lapsed — grace exhausted. Persistent danger bar should appear. */
const EXPIRED_STATUS = {
  ...HEALTHY_STATUS,
  validUpto: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
  expiryState: "Expired",
  graceEndsUtc: new Date(Date.now() - 16 * 24 * 60 * 60 * 1000).toISOString(),
};

const USAGE = [
  {
    id: "use-1",
    tenantId: "acme",
    periodYear: PERIOD_YEAR,
    periodMonth: PERIOD_MONTH,
    resource: "ApiCalls",
    usedUnits: 4200,
    limitUnits: 10000,
    overage: 0,
    capturedAtUtc: now.toISOString(),
  },
  {
    id: "use-2",
    tenantId: "acme",
    periodYear: PERIOD_YEAR,
    periodMonth: PERIOD_MONTH,
    resource: "StorageBytes",
    usedUnits: 900,
    limitUnits: 1000,
    overage: 120,
    capturedAtUtc: now.toISOString(),
  },
];

const INVOICE = {
  id: "inv-1",
  tenantId: "acme",
  invoiceNumber: "INV-2026-05",
  periodYear: 2026,
  periodMonth: 5,
  currency: "USD",
  subtotalAmount: 149,
  status: "Issued",
  createdAtUtc: "2026-05-01T00:00:00Z",
  issuedAtUtc: "2026-05-01T00:00:00Z",
  dueAtUtc: "2026-05-15T00:00:00Z",
  paidAtUtc: null,
  voidedAtUtc: null,
  notes: null,
  lineItems: [],
  purpose: "Subscription",
};

const INVOICE_DETAIL = {
  ...INVOICE,
  notes: "Thanks for your business.",
  lineItems: [
    {
      id: "li-1",
      kind: "BaseFee",
      resource: null,
      description: "Scale plan — monthly base fee",
      quantity: 1,
      unitPrice: 99,
      amount: 99,
    },
    {
      id: "li-2",
      kind: "Overage",
      resource: "StorageBytes",
      description: "Storage overage",
      quantity: 50,
      unitPrice: 1,
      amount: 50,
    },
  ],
};

// ── Subscription page ────────────────────────────────────────────────

test.describe("subscription (/subscription)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", HEALTHY_STATUS);
    await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", SUBSCRIPTION);
    await mockJsonResponse(page, "**/api/v1/billing/usage**", USAGE);
    await mockJsonResponse(page, "**/api/v1/billing/invoices/me**", paged([INVOICE]));
  });

  test("renders plan, usage rows, and a recent invoice link", async ({ page }) => {
    await page.goto("/subscription");

    await expect(page.getByRole("heading", { name: /subscription/i })).toBeVisible();

    // Plan name + status.
    await expect(page.getByText("Scale").first()).toBeVisible();

    // Usage section + both resources.
    await expect(page.getByText("Usage by resource", { exact: true })).toBeVisible();
    await expect(page.getByText("ApiCalls", { exact: true })).toBeVisible();
    await expect(page.getByText("StorageBytes", { exact: true })).toBeVisible();

    // Recent invoices section + a clickable invoice.
    await expect(page.getByText("Recent invoices", { exact: true })).toBeVisible();
    const invoiceLink = page.getByRole("link", { name: /INV-2026-05/i });
    await expect(invoiceLink).toBeVisible();
    await expect(invoiceLink).toHaveAttribute("href", "/invoices/inv-1");
  });

  test("shows the no-subscription empty state", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", null);
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", {
      ...HEALTHY_STATUS,
      plan: null,
    });
    await page.goto("/subscription");
    await expect(page.getByText(/no active subscription/i)).toBeVisible();
  });
});

// ── Expiry banner ──────────────────────────────────────────────────────

test.describe("expiry banner", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", SUBSCRIPTION);
    await mockJsonResponse(page, "**/api/v1/billing/usage**", []);
    await mockJsonResponse(page, "**/api/v1/audits**", paged([]));
  });

  test("warns while in grace", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", GRACE_STATUS);
    await page.goto("/");
    await expect(page.getByText(/your subscription expired/i)).toBeVisible();
    await expect(page.getByText(/grace left/i)).toBeVisible();
  });

  test("informs when nearing expiry", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", NEARING_STATUS);
    await page.goto("/");
    await expect(page.getByText(/your subscription expires in/i)).toBeVisible();
  });

  test("pins a persistent bar when expired (no dismiss)", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", EXPIRED_STATUS);
    await page.goto("/");
    await expect(page.getByText(/your subscription has expired/i)).toBeVisible();
    // Expired is the hardest state — it cannot be dismissed.
    await expect(
      page.getByRole("button", { name: /dismiss subscription notice/i }),
    ).toHaveCount(0);
  });

  test("is absent when healthy", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", HEALTHY_STATUS);
    await page.goto("/");
    // Wait for the page to settle, then assert the bar never showed.
    await expect(page.getByRole("heading", { name: /good (morning|afternoon|evening)/i })).toBeVisible();
    await expect(page.getByText(/your subscription expire/i)).toHaveCount(0);
  });

  test("can be dismissed for the session", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", GRACE_STATUS);
    await page.goto("/");
    await expect(page.getByText(/your subscription expired/i)).toBeVisible();
    await page.getByRole("button", { name: /dismiss subscription notice/i }).click();
    await expect(page.getByText(/your subscription expired/i)).toHaveCount(0);
  });
});

// ── Invoice detail ───────────────────────────────────────────────────

test.describe("invoice detail (/invoices/:id)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", HEALTHY_STATUS);
  });

  test("renders line items and totals", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/billing/invoices/inv-1", INVOICE_DETAIL);
    await page.goto("/invoices/inv-1");

    await expect(page.getByRole("heading", { name: /INV-2026-05/i })).toBeVisible();
    await expect(page.getByText("Scale plan — monthly base fee")).toBeVisible();
    await expect(page.getByText("Storage overage")).toBeVisible();
    await expect(page.getByText("Total", { exact: true })).toBeVisible();
    await expect(page.getByText("Thanks for your business.")).toBeVisible();
  });

  test("Download PDF button issues the /pdf request", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/billing/invoices/inv-1", INVOICE_DETAIL);

    // Intercept the PDF stream and fulfil with a tiny binary body so the
    // blob/anchor download path runs without a real backend.
    let pdfRequested = false;
    await page.route("**/api/v1/billing/invoices/inv-1/pdf", async (route) => {
      pdfRequested = true;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/pdf" },
        body: "%PDF-1.4 mock",
      });
    });

    await page.goto("/invoices/inv-1");
    await page.getByRole("button", { name: /download pdf/i }).click();

    await expect.poll(() => pdfRequested).toBe(true);
  });

  test("shows a not-found panel on 404", async ({ page }) => {
    await page.route("**/api/v1/billing/invoices/missing", (route) =>
      route.fulfill({
        status: 404,
        headers: { "Content-Type": "application/problem+json" },
        body: JSON.stringify({ status: 404, title: "Not Found" }),
      }),
    );
    await page.goto("/invoices/missing");
    await expect(page.getByText(/invoice not found/i)).toBeVisible();
  });
});
