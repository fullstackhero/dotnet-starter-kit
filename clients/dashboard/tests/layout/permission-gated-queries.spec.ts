import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { installShellMocks } from "../helpers/shell-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

// The topbar badges and the overview usage card query endpoints that each
// require a permission. A user without it must not fire the request at all:
// every attempt is a 403 the API logs as an error.

const GATED_ENDPOINTS = [
  "/api/v1/notifications/unread-count",
  "/api/v1/chat/channels",
  "/api/v1/billing/usage",
] as const;

const GRANTS = [
  "Permissions.Notifications.Inbox.View",
  "Permissions.Chat.Channels.View",
  "Permissions.Billing.View",
];

function countGatedRequests(page: Page): Record<string, number> {
  const counts: Record<string, number> = Object.fromEntries(GATED_ENDPOINTS.map((p) => [p, 0]));
  page.on("request", (req) => {
    const path = new URL(req.url()).pathname;
    if (path in counts) counts[path]++;
  });
  return counts;
}

test.describe("permission-gated background queries", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/billing/usage**", []);
    await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", null);
  });

  test("without the permissions, none of the gated endpoints is called", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", []);
    const counts = countGatedRequests(page);

    await page.goto("/");
    // Only renders once permissions are hydrated, so every query the page
    // mounts has had its chance to fire by now.
    await expect(page.getByText("No access to audits")).toBeVisible();

    expect(counts).toEqual(Object.fromEntries(GATED_ENDPOINTS.map((p) => [p, 0])));
    await expect(page.getByText("No access to usage")).toBeVisible();
    await expect(page.getByRole("button", { name: /^notifications/i })).toHaveCount(0);
    await expect(page.getByRole("button", { name: /^chat/i })).toHaveCount(0);
  });

  test("with the permissions, each gated endpoint is called and its UI renders", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", GRANTS);
    const counts = countGatedRequests(page);

    await page.goto("/");
    await expect(page.getByRole("button", { name: /^notifications/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /^chat/i })).toBeVisible();
    await expect(page.getByText(/no usage captured yet/i)).toBeVisible();

    for (const path of GATED_ENDPOINTS) {
      await expect.poll(() => counts[path], { message: path }).toBeGreaterThan(0);
    }
  });
});
