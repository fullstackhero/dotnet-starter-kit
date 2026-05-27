import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

const TICKET_ID = "00000000-0000-0000-0000-000000000abc";

const TICKET = {
  id: TICKET_ID,
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
};

const SEARCH_HITS = {
  items: [
    {
      id: "00000000-0000-0000-0000-000000000222",
      userName: "bob",
      firstName: "Bob",
      lastName: "Patel",
      email: "bob@acme.com",
      isActive: true,
      emailConfirmed: true,
    },
    {
      id: "00000000-0000-0000-0000-000000000333",
      userName: "carol",
      firstName: "Carol",
      lastName: "Smith",
      email: "carol@acme.com",
      isActive: true,
      emailConfirmed: true,
    },
  ],
  pageNumber: 1,
  pageSize: 8,
  totalCount: 2,
  totalPages: 1,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await mockJsonResponse(page, "**/api/v1/identity/profile", {
    id: TEST_USER.sub,
    userName: "alice",
    email: TEST_USER.email,
    firstName: TEST_USER.firstName,
    lastName: TEST_USER.lastName,
    isActive: true,
    emailConfirmed: true,
    twoFactorEnabled: false,
  });
  await mockJsonResponse(page, `**/api/v1/tickets/${TICKET_ID}`, TICKET);
  // listTicketComments returns a plain array, not a paged response.
  await mockJsonResponse(page, `**/api/v1/tickets/${TICKET_ID}/comments**`, []);
  await mockJsonResponse(
    page,
    "**/api/v1/identity/users/search**",
    SEARCH_HITS,
  );
});

test.describe("ticket assignee — user picker", () => {
  test("opens the Assign dialog and shows the typeahead search input", async ({ page }) => {
    await page.goto(`/tickets/${TICKET_ID}`);
    await page.getByRole("button", { name: /^Assign$/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByPlaceholder(/search by name or email/i)).toBeVisible();
  });

  test("typing fires a debounced search and renders results", async ({ page }) => {
    await page.goto(`/tickets/${TICKET_ID}`);
    await page.getByRole("button", { name: /^Assign$/i }).first().click();
    const dialog = page.getByRole("dialog");

    await dialog.getByPlaceholder(/search by name or email/i).fill("bob");

    // Debounce is 250ms — give the listbox time to materialise.
    await expect(dialog.getByRole("listbox")).toBeVisible({ timeout: 3_000 });
    await expect(dialog.getByRole("option").first()).toContainText(/bob patel/i);
    await expect(dialog.getByRole("option").first()).toContainText(/bob@acme\.com/i);
  });

  test("picking a result fills the selected chip with name + email", async ({ page }) => {
    await page.goto(`/tickets/${TICKET_ID}`);
    await page.getByRole("button", { name: /^Assign$/i }).first().click();
    const dialog = page.getByRole("dialog");

    await dialog.getByPlaceholder(/search by name or email/i).fill("bob");
    await dialog.getByRole("option", { name: /bob patel/i }).click();

    // Dropdown closes; chip with name + email is now visible above the
    // input, with a clear button.
    await expect(dialog.getByRole("listbox")).not.toBeVisible();
    await expect(dialog.getByText("Bob Patel")).toBeVisible();
    await expect(dialog.getByText("bob@acme.com")).toBeVisible();
    await expect(dialog.getByRole("button", { name: /clear/i })).toBeVisible();
  });

  test("submitting POSTs the picked id to the assign endpoint", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/tickets/${TICKET_ID}/assign`,
      TICKET,
    );

    await page.goto(`/tickets/${TICKET_ID}`);
    await page.getByRole("button", { name: /^Assign$/i }).first().click();
    const dialog = page.getByRole("dialog");

    await dialog.getByPlaceholder(/search by name or email/i).fill("bob");
    await dialog.getByRole("option", { name: /bob patel/i }).click();

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith(`/tickets/${TICKET_ID}/assign`) && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: /save assignment/i }).click();
    const req = await reqPromise;

    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      assigneeUserId: "00000000-0000-0000-0000-000000000222",
    });
  });

  test("Clear unselects the user (POSTs assignedToUserId: null)", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/tickets/${TICKET_ID}/assign`,
      TICKET,
    );

    await page.goto(`/tickets/${TICKET_ID}`);
    await page.getByRole("button", { name: /^Assign$/i }).first().click();
    const dialog = page.getByRole("dialog");

    await dialog.getByPlaceholder(/search by name or email/i).fill("bob");
    await dialog.getByRole("option", { name: /bob patel/i }).click();
    await dialog.getByRole("button", { name: /clear/i }).click();

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith(`/tickets/${TICKET_ID}/assign`) && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: /save assignment/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.assigneeUserId).toBeNull();
  });
});
