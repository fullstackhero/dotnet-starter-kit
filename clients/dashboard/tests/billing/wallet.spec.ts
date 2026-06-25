import { expect, test } from "@playwright/test";
import type { Route } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

// ── Fixtures ─────────────────────────────────────────────────────────

/** WalletDto — the tenant's prepaid WhatsApp balance. */
const WALLET = {
  id: "w-1",
  tenantId: "acme",
  currency: "USD",
  balance: 42.5,
  status: "Active",
  createdAtUtc: "2026-06-01T00:00:00Z",
  recentTransactions: [],
};

/** A TopupRequestDto already in flight (Pending) for the tenant. */
const EXISTING_REQUEST = {
  id: "tr-1",
  tenantId: "acme",
  amount: 250,
  currency: "USD",
  note: "June campaign budget",
  status: "Pending",
  invoiceId: null,
  requestedBy: "alice@acme.com",
  decisionNote: null,
  createdAtUtc: "2026-06-20T10:00:00Z",
  decidedAtUtc: null,
  completedAtUtc: null,
};

test.describe("wallet (/wallet)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("renders the balance and an existing pending top-up request", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/billing/wallet/me", WALLET);
    await mockJsonResponse(
      page,
      "**/api/v1/billing/wallet/topup-requests/me**",
      paged([EXISTING_REQUEST]),
    );

    await page.goto("/wallet");

    await expect(page.getByRole("heading", { name: /whatsapp wallet/i })).toBeVisible();

    // Balance card formats the WalletDto.balance as currency.
    await expect(page.getByText("$42.50")).toBeVisible();

    // The existing request shows in the list: amount + Pending status badge.
    // The page renders both a mobile card list (md:hidden) and a desktop table;
    // filter to the visible (desktop) copy at this viewport.
    await expect(page.getByText("$250.00").filter({ visible: true })).toBeVisible();
    await expect(page.getByText("Pending").filter({ visible: true }).first()).toBeVisible();
  });

  test("submitting the form POSTs the typed amount and the list refetch shows it", async ({
    page,
  }) => {
    // A mutable server-side list: the GET reads it at request time, the POST
    // appends to it — so the post-submit refetch surfaces the new request.
    const requests: Array<typeof EXISTING_REQUEST> = [EXISTING_REQUEST];

    await mockJsonResponse(page, "**/api/v1/billing/wallet/me", WALLET);

    // GET /topup-requests/me — returns the live list. Registered first so the
    // more-specific POST handler (registered after) wins for POSTs.
    await page.route("**/api/v1/billing/wallet/topup-requests/me**", async (route: Route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged(requests)),
      });
    });

    // POST /topup-requests — capture the body, append the new request, return its id.
    let postedBody: { amount?: number; note?: string } | null = null;
    await page.route("**/api/v1/billing/wallet/topup-requests", async (route: Route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      const raw = route.request().postData();
      postedBody = raw ? JSON.parse(raw) : null;
      requests.unshift({
        ...EXISTING_REQUEST,
        id: "tr-new",
        amount: postedBody?.amount ?? 0,
        note: postedBody?.note ?? null,
        createdAtUtc: new Date().toISOString(),
      });
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify("tr-new"),
      });
    });

    await page.goto("/wallet");
    await expect(page.getByRole("heading", { name: /whatsapp wallet/i })).toBeVisible();

    await page.locator("#topup-amount").fill("100");
    await page.locator("#topup-note").fill("Top up for July");
    await page.getByRole("button", { name: /request top-up/i }).click();

    // The POST fired with the typed amount + note.
    await expect.poll(() => postedBody).toMatchObject({ amount: 100, note: "Top up for July" });

    // Success toast confirms the request landed.
    await expect(page.getByText(/top-up requested/i)).toBeVisible();

    // The invalidated query refetches and the new $100.00 request appears.
    await expect(page.getByText("$100.00").filter({ visible: true })).toBeVisible();
  });
});
