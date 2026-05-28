import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// Branding card behaviour is owned by branding.spec.ts. This file covers the
// detail page header, status badge, and provisioning panel ONLY.

const TENANT_ID = "acme";

const TENANT = {
  id: TENANT_ID,
  name: "Acme Corp",
  adminEmail: "admin@acme.com",
  isActive: true,
  validUpto: "2027-01-01T00:00:00Z",
  issuer: "fsh.demo.acme",
};

const PROVISIONING = {
  tenantId: TENANT_ID,
  status: "Completed",
  currentStep: "CacheWarm",
  correlationId: "abc-123",
  createdUtc: "2026-05-10T10:00:00Z",
  startedUtc: "2026-05-10T10:00:00Z",
  completedUtc: "2026-05-10T10:00:08Z",
  error: null,
  steps: [
    {
      step: "SeedDatabase",
      status: "Completed",
      startedUtc: "2026-05-10T10:00:00Z",
      completedUtc: "2026-05-10T10:00:03Z",
    },
    {
      step: "SeedRoles",
      status: "Completed",
      startedUtc: "2026-05-10T10:00:03Z",
      completedUtc: "2026-05-10T10:00:08Z",
    },
  ],
};

const THEME_DEFAULT = {
  lightPalette: {},
  darkPalette: {},
  brandAssets: {},
  typography: { fontFamily: "Inter", headingFontFamily: "Inter", fontSizeBase: 14, lineHeightBase: 1.5 },
  layout: { borderRadius: "4px", defaultElevation: 1 },
  isDefault: true,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  // Branding card + impersonation grants fetch on the detail page — keep them
  // satisfied so the page renders, but we don't assert on them here.
  await mockJsonResponse(page, "**/api/v1/tenants/theme", THEME_DEFAULT);
  await mockJsonResponse(page, "**/api/v1/identity/impersonation/grants**", []);
});

test.describe("tenant detail header + provisioning", () => {
  test("renders the tenant header with name, id, email and active status", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/tenants/${TENANT_ID}/status`, TENANT);
    await mockJsonResponse(page, `**/api/v1/tenants/${TENANT_ID}/provisioning`, PROVISIONING);

    await page.goto(`/tenants/${TENANT_ID}`);

    // The name renders as both the page-header h1 and the card h2 — scope to
    // the card heading (level 2) to avoid a strict-mode collision.
    await expect(
      page.getByRole("heading", { level: 2, name: "Acme Corp", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Header chips: tenant id, admin email, active badge.
    await expect(page.getByText("acme", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("admin@acme.com", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("Active", { exact: true }).first()).toBeVisible();
  });

  test("renders the provisioning panel with the completed steps", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/tenants/${TENANT_ID}/status`, TENANT);
    await mockJsonResponse(page, `**/api/v1/tenants/${TENANT_ID}/provisioning`, PROVISIONING);

    await page.goto(`/tenants/${TENANT_ID}`);

    // Provisioning section heading — SettingsSection renders the title as an
    // <h2>. (The old mono "\ Provisioning" crumb was dropped in the reskin.)
    await expect(
      page.getByRole("heading", { level: 2, name: "Provisioning", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Overall status badge reflects Completed + current step.
    await expect(page.getByText(/Completed · CacheWarm/i)).toBeVisible();

    // The step rows from our mock render.
    await expect(page.getByText("SeedDatabase", { exact: true })).toBeVisible();
    await expect(page.getByText("SeedRoles", { exact: true })).toBeVisible();
  });

  test("surfaces a status load error in an error band", async ({ page }) => {
    await page.route(`**/api/v1/tenants/${TENANT_ID}/status`, (route) =>
      route.fulfill({
        status: 500,
        headers: { "Content-Type": "application/problem+json" },
        body: JSON.stringify({
          type: "https://httpstatuses.io/500",
          title: "Server error",
          status: 500,
          detail: "Tenant status is unavailable right now.",
        }),
      }),
    );
    await mockJsonResponse(page, `**/api/v1/tenants/${TENANT_ID}/provisioning`, PROVISIONING);

    await page.goto(`/tenants/${TENANT_ID}`);

    await expect(page.getByText(/Tenant status is unavailable right now\./i)).toBeVisible({
      timeout: 10_000,
    });
  });
});
