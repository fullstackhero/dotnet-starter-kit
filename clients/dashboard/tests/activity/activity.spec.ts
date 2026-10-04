import { expect, test, type Page, type Route } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// /activity shows two separate things:
//   - Live: SSE events held in memory since the tab connected (gone on refresh).
//   - Recent history: the persisted audit trail, loaded once from GET /audits —
//     only for users holding Permissions.AuditTrails.View (the endpoint 403s
//     otherwise, so users without it must never trigger the call).

const AUDIT_VIEW = "Permissions.AuditTrails.View";

const HISTORY_ROW = {
  id: "a-1",
  occurredAtUtc: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
  eventType: "EntityChange",
  severity: "Information",
  tenantId: "acme",
  userId: "u-test-1",
  userName: "Alice Nguyen",
  source: "CatalogDbContext",
  tags: 0,
};

async function grantPermissions(page: Page, perms: readonly string[]) {
  await mockJsonResponse(page, "**/api/v1/identity/permissions", perms);
}

/**
 * Track GET /audits requests. `sent` counts every attempt; `count` only the ones
 * that complete — the dev server runs React StrictMode, whose mount → unmount →
 * mount cancels the first in-flight fetch (a dev-only double request).
 */
function countAuditCalls(page: Page) {
  const counter = { count: 0, sent: 0 };
  const isAudits = (url: string) => new URL(url).pathname.startsWith("/api/v1/audits");
  page.on("request", (req) => {
    if (isAudits(req.url())) counter.sent++;
  });
  page.on("requestfinished", (req) => {
    if (isAudits(req.url())) counter.count++;
  });
  return counter;
}

/**
 * Serve one SSE event on the first stream connection, then abort every later
 * connection (reconnects, and the connection after a page reload) so the test
 * controls exactly what the live feed received.
 */
async function mockSseOnce(page: Page) {
  let served = false;
  await mockJsonResponse(page, "**/api/v1/sse/token", { token: "00000000-0000-0000-0000-000000000001" });
  await page.route("**/api/v1/sse/stream**", async (route: Route) => {
    if (served) {
      await route.abort();
      return;
    }
    served = true;
    await route.fulfill({
      status: 200,
      headers: { "Content-Type": "text/event-stream" },
      body: 'id: evt-1\nevent: ProductCreated\ndata: {"entityId":"prod-42"}\n\n',
    });
  });
}

test.describe("activity (/activity)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("with Audit trail access, history renders and survives a refresh", async ({ page }) => {
    await grantPermissions(page, [AUDIT_VIEW]);
    await mockJsonResponse(page, "**/api/v1/audits**", paged([HISTORY_ROW]));
    await mockSseOnce(page);
    const audits = countAuditCalls(page);

    await page.goto("/activity");
    await expect(page.getByRole("heading", { name: /live activity/i })).toBeVisible();

    // Live event and history row render in their own sections.
    const live = page.getByRole("region", { name: "Live", exact: true });
    const history = page.getByRole("region", { name: "Recent history", exact: true });
    await expect(live.getByText("ProductCreated").last()).toBeVisible();
    await expect(history.getByText("changed catalog records").last()).toBeVisible();
    // The live event is never folded into the history list.
    await expect(history.getByText("ProductCreated")).toHaveCount(0);

    await page.reload();

    // After a refresh the in-memory live feed is empty, but history is back.
    await expect(history.getByText("changed catalog records").last()).toBeVisible();
    await expect(live.getByText(/no events yet|listening for activity/i)).toBeVisible();
    await expect(live.getByText("ProductCreated")).toHaveCount(0);

    // One history request per page load — the live stream never triggers refetches.
    expect(audits.count).toBe(2);
  });

  test("history request drops the per-request system Activity noise", async ({ page }) => {
    await grantPermissions(page, [AUDIT_VIEW]);
    await mockJsonResponse(page, "**/api/v1/audits**", paged([HISTORY_ROW]));
    const request = page.waitForRequest((r) => new URL(r.url()).pathname === "/api/v1/audits");

    await page.goto("/activity");
    const url = new URL((await request).url());
    expect(url.searchParams.get("ExcludeEventType")).toBe("Activity");
    expect(url.searchParams.get("PageSize")).toBe("25");
  });

  test("refresh button refetches history on demand", async ({ page }) => {
    await grantPermissions(page, [AUDIT_VIEW]);
    await mockJsonResponse(page, "**/api/v1/audits**", paged([HISTORY_ROW]));
    const audits = countAuditCalls(page);

    await page.goto("/activity");
    await expect(page.getByText("changed catalog records").last()).toBeVisible();
    expect(audits.count).toBe(1);

    await page.getByRole("button", { name: /refresh history/i }).click();
    await expect.poll(() => audits.count).toBe(2);
  });

  test("without Audit trail access, no audits call and a live-only notice", async ({ page }) => {
    // installShellMocks grants no permissions ([]).
    await mockSseOnce(page);
    const audits = countAuditCalls(page);

    await page.goto("/activity");
    await expect(page.getByRole("heading", { name: /live activity/i })).toBeVisible();
    await expect(page.getByText("Showing live events only")).toBeVisible();
    await expect(page.getByRole("region", { name: "Recent history", exact: true })).toHaveCount(0);

    // Live events still work for this user.
    await expect(
      page.getByRole("region", { name: "Live", exact: true }).getByText("ProductCreated").last(),
    ).toBeVisible();

    expect(audits.sent).toBe(0);
  });

  test("renders the empty live state when the stream is offline", async ({ page }) => {
    await page.goto("/activity");
    await expect(page.getByRole("heading", { name: /live activity/i })).toBeVisible();
    await expect(page.getByText(/no events yet|listening for activity/i)).toBeVisible();
  });
});
