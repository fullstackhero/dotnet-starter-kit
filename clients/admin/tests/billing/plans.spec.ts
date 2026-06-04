import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// A BillingPlanDto matching src/api/billing.ts. The plans endpoint returns a
// bare array (not a PagedResponse).
const PLAN_PRO = {
  id: "plan-pro",
  key: "pro",
  name: "Pro",
  currency: "USD",
  monthlyBasePrice: 29,
  overageRates: { ApiCalls: 0.001 },
  isActive: true,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("billing plans list", () => {
  test("renders the All plans heading, a plan row, and the New plan button", async ({ page }) => {
    await page.route("**/api/v1/billing/plans?*", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify([PLAN_PRO]),
      });
    });

    await page.goto("/billing/plans");

    const main = page.getByRole("main");

    // CardTitle "All plans" identifies the list (scoped to main; the sidebar
    // nav also contains the word "Plans").
    await expect(main.getByText("All plans", { exact: true })).toBeVisible({ timeout: 10_000 });

    // The plan row from our mock: key code + display name + Active badge.
    await expect(main.getByText("pro", { exact: true })).toBeVisible();
    await expect(main.getByText("Pro", { exact: true })).toBeVisible();
    await expect(main.getByText("Active", { exact: true }).first()).toBeVisible();

    await expect(main.getByRole("button", { name: /new plan/i })).toBeVisible();
  });

  test("hides New plan + per-row Edit for a Billing.View-only user", async ({ page }) => {
    // Re-seed as a billing viewer: keep Billing.View (route guard), drop Billing.Manage.
    const viewOnly = ADMIN_PERMS.filter((p) => p !== "Permissions.Billing.Manage");
    await seedAuthedSession(page, { ...TEST_USER, permissions: viewOnly });
    await installAdminShellMocks(page, viewOnly);

    await page.route("**/api/v1/billing/plans?*", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify([PLAN_PRO]),
      });
    });

    await page.goto("/billing/plans");

    const main = page.getByRole("main");
    await expect(main.getByText("All plans", { exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText("Pro", { exact: true })).toBeVisible();

    // Manage affordances must be absent.
    await expect(main.getByRole("button", { name: /new plan/i })).toHaveCount(0);
    await expect(main.getByRole("button", { name: /edit pro/i })).toHaveCount(0);
  });

  test("shows the empty state when there are no plans", async ({ page }) => {
    await page.route("**/api/v1/billing/plans?*", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify([]),
      });
    });

    await page.goto("/billing/plans");

    await expect(
      page.getByText(/no plans yet/i),
    ).toBeVisible({ timeout: 10_000 });
  });
});

// Plan create/edit moved from page-forms (/billing/plans/new, /:id) to the
// PlanFormDialog opened from the list. Those flows are covered by the
// "plan dialog — billing interval" tests in ../tenants/tenant-billing.spec.ts.
