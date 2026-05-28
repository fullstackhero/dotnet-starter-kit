import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

// DashboardPage ("/") is protected. The RouteGuard reads the in-memory
// permission set, which the auth context re-hydrates from
// /identity/permissions after mount — installAdminShellMocks echoes ADMIN_PERMS
// from that endpoint, so the seeded perms and the helper's perms must match.
//
// On load the page fires three queries:
//   GET /api/v1/tenants/?PageNumber=1&PageSize=1   (totalCount drives "Tenants")
//   GET /api/v1/billing/plans?includeInactive=true (array, drives "Plans")
//   GET /api/v1/billing/invoices?pageNumber=1&pageSize=50 (paged, drives invoices)

const TENANTS_PAGE = paged(
  [
    {
      id: "acme",
      name: "Acme Corp",
      adminEmail: "admin@acme.com",
      isActive: true,
      validUpto: "2027-01-01T00:00:00Z",
    },
  ],
  { pageNumber: 1, pageSize: 1, totalCount: 12 },
);

const PLANS = [
  {
    id: "p-free",
    key: "free",
    name: "Free",
    currency: "USD",
    monthlyBasePrice: 0,
    overageRates: {},
    isActive: true,
  },
  {
    id: "p-pro",
    key: "pro",
    name: "Pro",
    currency: "USD",
    monthlyBasePrice: 49,
    overageRates: {},
    isActive: true,
  },
  {
    id: "p-legacy",
    key: "legacy",
    name: "Legacy",
    currency: "USD",
    monthlyBasePrice: 19,
    overageRates: {},
    isActive: false,
  },
];

const INVOICES_PAGE = paged(
  [
    {
      id: "inv-1",
      tenantId: "acme",
      invoiceNumber: "INV-0001",
      periodYear: 2026,
      periodMonth: 5,
      currency: "USD",
      subtotalAmount: 49,
      status: "Issued",
      createdAtUtc: "2026-05-01T00:00:00Z",
      lineItems: [],
    },
    {
      id: "inv-2",
      tenantId: "acme",
      invoiceNumber: "INV-0002",
      periodYear: 2026,
      periodMonth: 4,
      currency: "USD",
      subtotalAmount: 49,
      status: "Paid",
      createdAtUtc: "2026-04-01T00:00:00Z",
      lineItems: [],
    },
  ],
  { pageNumber: 1, pageSize: 50, totalCount: 134 },
);

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);

  // Page-specific mocks AFTER the shell mocks so they win.
  await mockJsonResponse(page, "**/api/v1/tenants**", TENANTS_PAGE);
  await mockJsonResponse(page, "**/api/v1/billing/plans**", PLANS);
  await mockJsonResponse(page, "**/api/v1/billing/invoices**", INVOICES_PAGE);
});

test.describe("admin dashboard", () => {
  test("greets the operator by first name in the hero heading", async ({ page }) => {
    await page.goto("/");

    // Seeded user is "Root Admin" → first name "Root". The EntityPageHeader h1
    // renders "Overview" + a muted ", Root" subspan, so match the accessible name.
    await expect(
      page.getByRole("heading", { name: /Overview,\s*Root/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("renders the four KPI tiles with values from the load endpoints", async ({ page }) => {
    await page.goto("/");

    // Scope to the page content region — the KPI labels ("Tenants", "Plans")
    // also appear in the sidebar nav, so an unscoped getByText collides.
    const main = page.getByRole("main");

    // KPI tile labels render as the Stat component's mono-caps ".meta" crumb.
    // "Tenants"/"Plans" also appear as pivot-card titles, so target the label
    // element by its class rather than a bare text match.
    const kpiLabel = (text: string) =>
      main.locator("div.meta", { hasText: text });
    await expect(kpiLabel("Tenants")).toBeVisible({ timeout: 10_000 });
    await expect(kpiLabel("Plans")).toBeVisible();
    await expect(kpiLabel("Invoices")).toBeVisible();
    await expect(kpiLabel("Outstanding")).toBeVisible();

    // Values: tenants totalCount = 12, plans length = 3, invoices on page = 2,
    // outstanding (status === "Issued") = 1.
    await expect(main.getByText("12", { exact: true })).toBeVisible();
    await expect(main.getByText("2 active")).toBeVisible();
    await expect(main.getByText("134 total ledger")).toBeVisible();
  });

  test("renders the entry-point pivot cards", async ({ page }) => {
    await page.goto("/");

    // The sidebar nav lives OUTSIDE <main>, so scoping to the content region
    // isolates the four pivot-card links from the nav's own route links.
    const main = page.getByRole("main");
    await expect(main.getByText("Entry points")).toBeVisible({ timeout: 10_000 });

    await expect(main.getByRole("link", { name: /Tenants/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /Users/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /Billing/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /Invoices/ })).toBeVisible();
  });
});
