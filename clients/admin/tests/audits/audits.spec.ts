import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

const AUDIT_ROW = {
  id: "aud-1111-2222-3333",
  occurredAtUtc: "2026-05-20T14:22:01Z",
  eventType: 2, // Security (int form — the API boundary normalizes it)
  severity: 5, // Error
  tenantId: "root",
  userId: "u-77",
  userName: "rootadmin",
  traceId: "trace-abc",
  correlationId: "corr-xyz-001",
  requestId: "req-9",
  source: "POST /api/v1/identity/token",
  tags: 0,
};

const AUDIT_DETAIL = {
  ...AUDIT_ROW,
  receivedAtUtc: "2026-05-20T14:22:02Z",
  spanId: "span-def",
  payload: { action: "login", outcome: "success" },
};

const SUMMARY = {
  eventsByType: { Security: 12, Exception: 3, EntityChange: 40 },
  eventsBySeverity: { Information: 50, Error: 4, Critical: 1 },
  eventsBySource: { "POST /api/v1/identity/token": 12 },
  eventsByTenant: { root: 55 },
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("audit trail list", () => {
  test("renders the heading, KPI strip, and an audit row from the mock", async ({ page }) => {
    // Register the more-specific summary glob LAST so it wins over the list glob.
    await mockJsonResponse(page, "**/api/v1/audits/?*", paged([AUDIT_ROW], { pageSize: 25 }));
    await mockJsonResponse(page, "**/api/v1/audits/summary*", SUMMARY);

    await page.goto("/audits");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Audit trail", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // KPI strip labels.
    await expect(main.getByText("Total events", { exact: true })).toBeVisible();
    await expect(main.getByText("Errors + critical", { exact: true })).toBeVisible();
    await expect(main.getByText("Security events", { exact: true })).toBeVisible();
    await expect(main.getByText("Exceptions", { exact: true })).toBeVisible();

    // The audit row from our mock (source + correlation).
    await expect(main.getByText("POST /api/v1/identity/token", { exact: true })).toBeVisible();
    await expect(main.getByText(/corr · corr-xyz-001/)).toBeVisible();
  });

  test("shows the empty state when no events match", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/audits/?*", paged([], { pageSize: 25 }));
    await mockJsonResponse(page, "**/api/v1/audits/summary*", SUMMARY);

    await page.goto("/audits");

    const main = page.getByRole("main");
    await expect(
      main.getByText("No audit events match your filters.", { exact: true }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("has the event-type and severity filters", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/audits/?*", paged([AUDIT_ROW], { pageSize: 25 }));
    await mockJsonResponse(page, "**/api/v1/audits/summary*", SUMMARY);

    await page.goto("/audits");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Audit trail", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // The // Filters rail with its two native <select> comboboxes. Options are
    // hidden inside a native select, so assert the comboboxes + their default
    // empty-label option text (attached, not visible) instead.
    await expect(main.getByText("// Filters", { exact: true })).toBeVisible();
    const combos = main.getByRole("combobox");
    await expect(combos).toHaveCount(2);
    await expect(
      main.getByRole("option", { name: "All event types" }),
    ).toBeAttached();
    await expect(
      main.getByRole("option", { name: "All severities" }),
    ).toBeAttached();
    // Search box.
    await expect(
      main.getByPlaceholder("Search user, source, correlation…"),
    ).toBeVisible();
  });
});

test.describe("audit detail", () => {
  test("loads the forensic record with correlation + payload", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/audits/${AUDIT_DETAIL.id}`, AUDIT_DETAIL);

    await page.goto(`/audits/${AUDIT_DETAIL.id}`);

    const main = page.getByRole("main");
    // h1 renders "<EventType> event" — Security → "Security event".
    await expect(
      main.getByRole("heading", { name: "Security event", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Correlation chip + value.
    await expect(main.getByText("Correlation id", { exact: true })).toBeVisible();
    await expect(main.getByText("corr-xyz-001", { exact: true })).toBeVisible();

    // Payload pane shows the JSON we returned.
    await expect(main.getByText(/"action": "login"/)).toBeVisible();
  });
});
