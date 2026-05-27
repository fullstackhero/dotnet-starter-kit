import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const TICKETS = [
  {
    id: "00000000-0000-0000-0000-0000000000a1",
    number: "TK-1",
    title: "Login button broken on mobile",
    description: "Tap-target collision on iOS Safari.",
    status: "Open",
    priority: "High",
    reporterUserId: "00000000-0000-0000-0000-000000000111",
    assignedToUserId: null,
    resolutionNote: null,
    createdAtUtc: "2026-05-10T10:00:00Z",
    updatedAtUtc: null,
    resolvedAtUtc: null,
    closedAtUtc: null,
    commentCount: 0,
    deletedOnUtc: null,
    deletedBy: null,
  },
  {
    id: "00000000-0000-0000-0000-0000000000a2",
    number: "TK-2",
    title: "Export CSV times out on large tenants",
    description: null,
    status: "InProgress",
    priority: "Critical",
    reporterUserId: "00000000-0000-0000-0000-000000000222",
    assignedToUserId: null,
    resolutionNote: null,
    createdAtUtc: "2026-05-09T08:00:00Z",
    updatedAtUtc: "2026-05-11T08:00:00Z",
    resolvedAtUtc: null,
    closedAtUtc: null,
    commentCount: 3,
    deletedOnUtc: null,
    deletedBy: null,
  },
];

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
  // The create dialog can search users; keep it empty + harmless.
  await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged([]));
  // Default tickets list — registered AFTER the shell so it wins.
  await mockJsonResponse(page, "**/api/v1/tickets**", paged(TICKETS));
});

test.describe("tickets — list page", () => {
  test("renders the heading and a ticket row with status + priority badges", async ({ page }) => {
    await page.goto("/tickets");

    await expect(page.getByRole("heading", { name: /tickets/i })).toBeVisible();

    // List text appears in BOTH the hidden mobile card and the desktop row;
    // scope to the desktop list card so the assertions land on one element.
    const card = page.locator(".md\\:block").filter({ hasText: "Subject" });
    await expect(card.getByText("Login button broken on mobile")).toBeVisible();
    await expect(card.getByText("TK-1", { exact: true })).toBeVisible();

    // Status + priority badges for the first ticket.
    await expect(card.getByText("High", { exact: true })).toBeVisible();
    await expect(card.getByText("Open", { exact: true })).toBeVisible();

    // Second ticket — In-progress / Critical.
    await expect(card.getByText("Export CSV times out on large tenants")).toBeVisible();
    await expect(card.getByText("Critical", { exact: true })).toBeVisible();
    await expect(card.getByText("In progress", { exact: true })).toBeVisible();
  });

  test("shows the empty state when there are no tickets", async ({ page }) => {
    // Override the default list mock with an empty page.
    await mockJsonResponse(page, "**/api/v1/tickets**", paged([]));

    await page.goto("/tickets");

    await expect(page.getByRole("heading", { name: /no tickets yet/i })).toBeVisible();
    await expect(
      page.getByText(/Open the first ticket to start tracking work\./i),
    ).toBeVisible();
  });

  test("typing in search sends the term as a query param", async ({ page }) => {
    await page.goto("/tickets");

    // Wait for the first load to settle so the next request is the search.
    const card = page.locator(".md\\:block").filter({ hasText: "Subject" });
    await expect(card.getByText("Login button broken on mobile")).toBeVisible();

    const searchReq = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/tickets?") &&
        new URL(r.url()).searchParams.get("search") === "csv",
      { timeout: 5_000 },
    );

    await page
      .getByPlaceholder(/find by number, title, or description/i)
      .fill("csv");

    await searchReq;
  });

  test("a status filter narrows the query", async ({ page }) => {
    await page.goto("/tickets");

    const card = page.locator(".md\\:block").filter({ hasText: "Subject" });
    await expect(card.getByText("Login button broken on mobile")).toBeVisible();

    const statusGroup = page.getByRole("group", { name: /status filter/i });

    const filteredReq = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/tickets?") &&
        new URL(r.url()).searchParams.get("status") === "Open",
      { timeout: 5_000 },
    );

    await statusGroup.getByRole("button", { name: "Open", exact: true }).click();

    await filteredReq;
  });

  test("opens the New ticket dialog with a title field", async ({ page }) => {
    await page.goto("/tickets");

    await page.getByRole("button", { name: /new ticket/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/open a ticket/i)).toBeVisible();
    // Required-field label gets an sr-only "required" suffix, so match by
    // prefix rather than exact text.
    await expect(dialog.getByLabel(/^title/i)).toBeVisible();
  });
});
