import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// The API serializes enums as their string name (global JsonStringEnumConverter).
// AuditEventType: EntityChange | Security | Activity | Exception
// AuditSeverity: Information | Warning | Error | Critical
type AuditRow = {
  id: string;
  occurredAtUtc: string;
  eventType: string;
  severity: string;
  tenantId?: string | null;
  userId?: string | null;
  userName?: string | null;
  traceId?: string | null;
  correlationId?: string | null;
  requestId?: string | null;
  source?: string | null;
  tags: number;
};

function row(over: Partial<AuditRow> = {}): AuditRow {
  return {
    id: "a-1",
    occurredAtUtc: "2026-05-20T14:32:11.234Z",
    eventType: "Security",
    severity: "Error",
    tenantId: "acme",
    userId: "11111111-2222-3333-4444-555555555555",
    userName: "Alice Nguyen",
    source: "api.identity.RegisterUser",
    tags: 0,
    ...over,
  };
}

async function mockSummary(page: import("@playwright/test").Page) {
  // The list glob "**/api/v1/audits**" also matches "/audits/summary", so
  // register the summary mock AFTER the list mock — most-recently-registered
  // route wins in Playwright, so the summary call resolves to this shape.
  await mockJsonResponse(page, "**/api/v1/audits/summary**", {
    eventsByType: { Security: 3, Activity: 10 },
    eventsBySeverity: { Information: 8, Error: 5 },
    eventsBySource: { "api.identity.RegisterUser": 4 },
    eventsByTenant: { acme: 13 },
  });
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("system/audits", () => {
  test("renders the 'Audit trail' heading + a desktop audit row", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/audits**",
      paged([row({ source: "api.identity.RegisterUser", userName: "Alice Nguyen" })], {
        totalCount: 1,
      }),
    );
    await mockSummary(page);

    await page.goto("/system/audits");

    await expect(
      page.getByRole("heading", { name: "Audit trail", level: 1 }),
    ).toBeVisible();

    // The source text renders in both the (hidden) mobile card and the
    // desktop row — assert on the last (visible desktop) occurrence.
    await expect(
      page.getByText("api.identity.RegisterUser").last(),
    ).toBeVisible();
    await expect(page.getByText(/1 event found/i)).toBeVisible();
  });

  test("renders the empty state when no audits are in the window", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/audits**", paged<AuditRow>([], { totalCount: 0 }));
    await mockSummary(page);

    await page.goto("/system/audits");

    await expect(
      page.getByRole("heading", { name: /no audits in this window/i }),
    ).toBeVisible();
    await expect(page.getByText(/try widening the time range/i)).toBeVisible();
  });

  test("clicking an event-type chip re-queries with the eventType filter", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/audits**",
      paged([row()], { totalCount: 1 }),
    );
    await mockSummary(page);

    await page.goto("/system/audits");
    await expect(
      page.getByRole("heading", { name: "Audit trail", level: 1 }),
    ).toBeVisible();

    // Security → EventType=Security in the query string.
    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/audits?") &&
        /[?&]EventType=Security(&|$)/.test(r.url()),
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: "Security", exact: true }).click();
    await reqPromise;
  });

  test("opening a row slides in the detail drawer", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/audits**",
      paged([row({ id: "a-42", source: "api.catalog.CreateProduct" })], {
        totalCount: 1,
      }),
    );
    await mockSummary(page);
    // Detail endpoint — registered after the list glob so it wins for /{id}.
    await mockJsonResponse(page, "**/api/v1/audits/a-42", {
      ...row({ id: "a-42", source: "api.catalog.CreateProduct" }),
      receivedAtUtc: "2026-05-20T14:32:11.300Z",
      payload: { entity: "Product", action: "Created" },
    });

    await page.goto("/system/audits");

    await page.getByText("api.catalog.CreateProduct").last().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    // The drawer body renders an Identity section + the source value.
    await expect(dialog.getByText("Identity", { exact: true })).toBeVisible();
    await expect(dialog.getByText("Payload", { exact: true })).toBeVisible();
  });
});
