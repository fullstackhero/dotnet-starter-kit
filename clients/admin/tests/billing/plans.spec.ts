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

  test("the New plan button navigates to the create form", async ({ page }) => {
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
    await expect(main.getByRole("button", { name: /new plan/i })).toBeVisible({ timeout: 10_000 });

    await main.getByRole("button", { name: /new plan/i }).click();

    await expect(page).toHaveURL(/\/billing\/plans\/new$/);
    await expect(main.getByRole("heading", { name: "New plan", exact: true })).toBeVisible();
  });
});

test.describe("billing plan create form", () => {
  test("renders the create fields and POSTs a new plan", async ({ page }) => {
    // List query is fired by the list page only; the create form does not need
    // it (enabled: isEdit === false). Still safe to leave unmocked here.
    await page.route("**/api/v1/billing/plans", async (route) => {
      // Bare endpoint (no query) is the create POST target.
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify("plan-new"),
      });
    });
    // After a successful create the page navigates to /billing/plans which
    // re-fetches the (query-string) list — mock it so navigation is clean.
    await page.route("**/api/v1/billing/plans?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify([]),
      });
    });

    await page.goto("/billing/plans/new");

    const main = page.getByRole("main");
    await expect(main.getByRole("heading", { name: "New plan", exact: true })).toBeVisible({
      timeout: 10_000,
    });

    // Field labels (getByLabel matches the htmlFor association).
    await main.getByLabel(/^Key/).fill("starter");
    await main.getByLabel(/^Display name/).fill("Starter");
    await main.getByLabel(/^Monthly base price/).fill("19.00");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/billing/plans") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await main.getByRole("button", { name: /create plan/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.key).toBe("starter");
    expect(body.name).toBe("Starter");
    expect(body.currency).toBe("USD");
    expect(body.monthlyBasePrice).toBe(19);
  });
});

test.describe("billing plan edit form", () => {
  test("loads the existing plan and PUTs the update", async ({ page }) => {
    // Edit mode hydrates from the includeInactive list and finds by id.
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
    await page.route("**/api/v1/billing/plans/plan-pro", async (route) => {
      if (route.request().method() !== "PUT") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify("plan-pro"),
      });
    });

    await page.goto("/billing/plans/plan-pro");

    const main = page.getByRole("main");
    await expect(main.getByRole("heading", { name: "Edit plan", exact: true })).toBeVisible({
      timeout: 10_000,
    });

    // Existing plan hydrated the form: the display-name field shows "Pro".
    const nameField = main.getByLabel(/^Display name/);
    await expect(nameField).toHaveValue("Pro");

    // Key + currency are disabled in edit mode.
    await expect(main.getByLabel(/^Key/)).toBeDisabled();

    await nameField.fill("Pro Plus");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/billing/plans/plan-pro") && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await main.getByRole("button", { name: /save changes/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.planId).toBe("plan-pro");
    expect(body.name).toBe("Pro Plus");
  });
});
