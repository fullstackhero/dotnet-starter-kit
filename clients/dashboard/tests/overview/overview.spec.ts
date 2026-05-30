import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

const SUBSCRIPTION = {
  id: "sub-1",
  tenantId: "acme",
  planId: "plan-scale",
  planKey: "Scale",
  startUtc: new Date(Date.UTC(2026, 0, 1)).toISOString(),
  endUtc: null,
  status: "Active",
};

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
};

test.describe("overview (/)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/billing/usage**", []);
    await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", SUBSCRIPTION);
    await mockJsonResponse(page, "**/api/v1/audits**", paged([]));
  });

  test("renders the greeting header and the four stat cards", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: /good (morning|afternoon|evening), alice/i })).toBeVisible();
    await expect(page.getByText("Plan", { exact: true })).toBeVisible();
    await expect(page.getByText("Resources", { exact: true })).toBeVisible();
    await expect(page.getByText("Live events", { exact: true })).toBeVisible();
  });

  test("renders the recent-audits + usage + system-status sections", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByText("Recent audits", { exact: true })).toBeVisible();
    await expect(page.getByText("Usage by resource", { exact: true })).toBeVisible();
    await expect(page.getByText("System status", { exact: true })).toBeVisible();
    // Empty usage → calm empty state, not a crash.
    await expect(page.getByText(/no usage captured yet/i)).toBeVisible();
  });

  test("Valid-for card reflects an in-grace tenant", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", {
      id: "acme",
      name: "Acme Corp",
      isActive: true,
      validUpto: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
      hasConnectionString: false,
      adminEmail: "admin@acme.com",
      issuer: null,
      plan: "Scale",
      expiryState: "InGrace",
      graceEndsUtc: new Date(Date.now() + 5 * 24 * 60 * 60 * 1000).toISOString(),
    });
    await page.goto("/");
    await expect(page.getByText("Valid for", { exact: true })).toBeVisible();
    // Grace surfaces the grace-end caption on the stat card.
    await expect(page.getByText(/grace ends/i)).toBeVisible();
  });

  test("Valid-for card reflects an expired tenant", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/me/status**", {
      id: "acme",
      name: "Acme Corp",
      isActive: false,
      validUpto: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
      hasConnectionString: false,
      adminEmail: "admin@acme.com",
      issuer: null,
      plan: "Scale",
      expiryState: "Expired",
      graceEndsUtc: new Date(Date.now() - 16 * 24 * 60 * 60 * 1000).toISOString(),
    });
    await page.goto("/");
    await expect(page.getByText("Valid for", { exact: true })).toBeVisible();
    // The stat card reads "Expired" rather than a healthy day count.
    await expect(page.getByText("Expired", { exact: true })).toBeVisible();
  });
});

test.describe("activity (/activity)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("renders the live-activity page with its empty state (stream offline in tests)", async ({ page }) => {
    await page.goto("/activity");
    await expect(page.getByRole("heading", { name: /live activity/i })).toBeVisible();
    await expect(page.getByText(/no events yet|listening for activity/i)).toBeVisible();
  });
});

test.describe("invoices (/invoices)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("renders an invoice row from the API", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/billing/invoices/me**", paged([INVOICE]));
    await page.goto("/invoices");
    await expect(page.getByRole("heading", { name: /invoices/i })).toBeVisible();
    // Invoice number + status render in both a (hidden) mobile card and the
    // desktop table row; the desktop one is last in the DOM on a wide viewport.
    await expect(page.getByText("INV-2026-05").last()).toBeVisible();
    await expect(page.getByText("Issued").last()).toBeVisible();
  });

  test("shows the empty state when there are no invoices", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/billing/invoices/me**", paged([]));
    await page.goto("/invoices");
    await expect(page.getByText(/no invoices yet/i)).toBeVisible();
  });

  test("filters by search term", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/billing/invoices/me**", paged([INVOICE]));
    await page.goto("/invoices");
    await expect(page.getByText("INV-2026-05").last()).toBeVisible();
    await page.getByPlaceholder(/search by invoice number/i).fill("nomatch-xyz");
    await expect(page.getByText(/no invoices found/i)).toBeVisible();
  });

  test("paginates across pages using the PagedResult envelope", async ({ page }) => {
    const PAGE_1 = { ...INVOICE, id: "inv-1", invoiceNumber: "INV-2026-05" };
    const PAGE_2 = { ...INVOICE, id: "inv-2", invoiceNumber: "INV-2026-04", periodMonth: 4 };

    // Serve page 1 or page 2 based on the requested pageNumber so the next
    // control drives a real envelope transition. totalCount=2, totalPages=2.
    await page.route("**/api/v1/billing/invoices/me**", async (route) => {
      const url = new URL(route.request().url());
      const pageNumber = Number(url.searchParams.get("pageNumber") ?? "1");
      const item = pageNumber >= 2 ? PAGE_2 : PAGE_1;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(
          paged([item], { pageNumber, pageSize: 20, totalCount: 2, totalPages: 2 }),
        ),
      });
    });

    await page.goto("/invoices");
    // Header reflects the TRUE total, not the loaded page size.
    await expect(page.getByText(/showing 1 of 2 invoices/i)).toBeVisible();
    await expect(page.getByText("INV-2026-05").last()).toBeVisible();
    await expect(page.getByText("Page 1 of 2", { exact: true })).toBeVisible();

    await page.getByRole("button", { name: /next page/i }).click();

    await expect(page.getByText("INV-2026-04").last()).toBeVisible();
    await expect(page.getByText("Page 2 of 2", { exact: true })).toBeVisible();
  });
});
