import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

type SessionRow = {
  id: string;
  userId?: string | null;
  userName?: string | null;
  userEmail?: string | null;
  ipAddress?: string | null;
  deviceType?: string | null;
  browser?: string | null;
  browserVersion?: string | null;
  operatingSystem?: string | null;
  osVersion?: string | null;
  createdAt: string;
  lastActivityAt: string;
  expiresAt: string;
  isActive: boolean;
  isCurrentSession: boolean;
};

function session(over: Partial<SessionRow> = {}): SessionRow {
  return {
    id: "s-1",
    userId: "u-9",
    userName: "Bob Carter",
    userEmail: "bob@acme.com",
    ipAddress: "203.0.113.7",
    deviceType: "Desktop",
    browser: "Chrome",
    browserVersion: "124",
    operatingSystem: "Windows",
    osVersion: "11",
    createdAt: "2026-05-20T10:00:00.000Z",
    lastActivityAt: "2026-05-20T14:30:00.000Z",
    expiresAt: "2026-05-21T10:00:00.000Z",
    isActive: true,
    isCurrentSession: false,
    ...over,
  };
}

// The sessions list lives under /api/v1/identity/sessions. The shell mock
// already covers /identity/profile and /identity/permissions; the sessions
// glob is registered AFTER the shell so it wins.
const SESSIONS = "**/api/v1/identity/sessions**";

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("system/sessions", () => {
  test("renders the 'Active sessions' heading + a session row (browser + IP)", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      SESSIONS,
      paged([session({ browser: "Chrome", browserVersion: "124", ipAddress: "203.0.113.7" })], {
        totalCount: 1,
      }),
    );

    await page.goto("/system/sessions");

    await expect(
      page.getByRole("heading", { name: "Active sessions", level: 1 }),
    ).toBeVisible();

    // Browser string is "Chrome 124"; rendered in both the hidden mobile
    // card and the visible desktop row → assert on the last occurrence.
    await expect(page.getByText("Chrome 124").last()).toBeVisible();
    await expect(page.getByText("203.0.113.7").last()).toBeVisible();
    await expect(page.getByText(/1 session found/i)).toBeVisible();
  });

  test("flags the current session with a 'You' badge", async ({ page }) => {
    await mockJsonResponse(
      page,
      SESSIONS,
      paged([session({ id: "s-me", isCurrentSession: true, userName: "Alice Nguyen" })], {
        totalCount: 1,
      }),
    );

    await page.goto("/system/sessions");

    await expect(page.getByText("You", { exact: true }).last()).toBeVisible();
  });

  test("renders the empty state when there are no sessions", async ({ page }) => {
    await mockJsonResponse(page, SESSIONS, paged<SessionRow>([], { totalCount: 0 }));

    await page.goto("/system/sessions");

    await expect(
      page.getByRole("heading", { name: /no active sessions/i }),
    ).toBeVisible();
    await expect(page.getByText(/sessions appear when users sign in/i)).toBeVisible();
  });

  test("toggling 'Include inactive' re-queries with includeInactive=true", async ({ page }) => {
    await mockJsonResponse(
      page,
      SESSIONS,
      paged([session()], { totalCount: 1 }),
    );

    await page.goto("/system/sessions");
    await expect(
      page.getByRole("heading", { name: "Active sessions", level: 1 }),
    ).toBeVisible();

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/sessions") &&
        r.url().includes("includeInactive=true"),
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /include inactive/i }).click();
    await reqPromise;
  });
});
