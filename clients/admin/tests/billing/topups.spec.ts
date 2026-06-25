import { expect, test } from "@playwright/test";
import type { Route } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

// A TopupRequestDto matching src/api/wallet.ts, awaiting an operator decision.
const PENDING_REQUEST = {
  id: "tr-9",
  tenantId: "acme",
  amount: 500,
  currency: "USD",
  note: "campaign budget",
  status: "Pending",
  invoiceId: null,
  requestedBy: "alice@acme.com",
  decisionNote: null,
  createdAtUtc: "2026-06-20T10:00:00Z",
  decidedAtUtc: null,
  completedAtUtc: null,
};

test.beforeEach(async ({ page }) => {
  // ADMIN_PERMS carries Permissions.Billing.View + Permissions.Billing.Manage,
  // so the Approve/Reject row actions render.
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("billing top-ups list", () => {
  test("renders a pending request with an Approve action", async ({ page }) => {
    await page.route("**/api/v1/billing/wallet/topup-requests?*", async (route: Route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([PENDING_REQUEST])),
      });
    });

    await page.goto("/billing/topups");

    // The list card + the request row (amount + tenant + Approve button).
    await expect(page.getByText("Top-up requests", { exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText("$500.00").first()).toBeVisible();
    await expect(page.getByText(/tenant\s+acme/i)).toBeVisible();
    await expect(page.getByRole("button", { name: "Approve", exact: true })).toBeVisible();
  });

  test("approving a request POSTs to /approve and shows the invoice toast", async ({ page }) => {
    await page.route("**/api/v1/billing/wallet/topup-requests?*", async (route: Route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([PENDING_REQUEST])),
      });
    });

    // POST /topup-requests/{id}/approve → returns the generated invoice id.
    let approvedId: string | null = null;
    await page.route(
      "**/api/v1/billing/wallet/topup-requests/*/approve",
      async (route: Route) => {
        if (route.request().method() !== "POST") {
          await route.fallback();
          return;
        }
        approvedId = route.request().url();
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify("inv-new-1"),
        });
      },
    );

    await page.goto("/billing/topups");
    await expect(page.getByText("$500.00").first()).toBeVisible({ timeout: 10_000 });

    // Open the confirm dialog from the row action, then confirm.
    await page.getByRole("button", { name: "Approve", exact: true }).click();
    await page
      .getByRole("button", { name: /approve & generate invoice/i })
      .click();

    // The approve endpoint fired for the right request.
    await expect.poll(() => approvedId).toContain("/topup-requests/tr-9/approve");

    // Success toast + the "View invoice" action linking to the new invoice.
    await expect(page.getByText(/invoice generated/i)).toBeVisible();
    await expect(page.getByText(/view invoice/i)).toBeVisible();
  });
});
