import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// SessionsSettings lists the current user's sessions via
// GET /api/v1/identity/sessions/me, revokes one via DELETE
// /api/v1/identity/sessions/{id}, and signs out the rest via
// POST /api/v1/identity/sessions/revoke-all.

const now = Date.now();
const iso = (offsetMs: number) => new Date(now + offsetMs).toISOString();

const CURRENT_SESSION = {
  id: "sess-current",
  ipAddress: "10.0.0.1",
  deviceType: "Desktop",
  browser: "Chrome",
  browserVersion: "120",
  operatingSystem: "Windows",
  createdAt: iso(-3_600_000),
  lastActivityAt: iso(-5_000),
  expiresAt: iso(86_400_000),
  isActive: true,
  isCurrentSession: true,
};

const OTHER_SESSION = {
  id: "sess-other",
  ipAddress: "203.0.113.7",
  deviceType: "Mobile",
  browser: "Safari",
  browserVersion: "17",
  operatingSystem: "iOS",
  createdAt: iso(-7_200_000),
  lastActivityAt: iso(-600_000),
  expiresAt: iso(86_400_000),
  isActive: true,
  isCurrentSession: false,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("settings · sessions", () => {
  test("renders a session row for the current device and another device", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/sessions/me", [
      CURRENT_SESSION,
      OTHER_SESSION,
    ]);

    await page.goto("/settings/sessions");

    const main = page.getByRole("main");
    await expect(main.getByText(/Active sessions/)).toBeVisible({ timeout: 10_000 });

    // Both device rows surface their browser-on-os summary.
    await expect(main.getByText(/Chrome 120 on Windows/)).toBeVisible();
    await expect(main.getByText(/Safari 17 on iOS/)).toBeVisible();
    // Current session is badged and has no Revoke button.
    await expect(main.getByText("This device", { exact: true })).toBeVisible();
  });

  test("shows a revoke affordance for non-current sessions", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/sessions/me", [
      CURRENT_SESSION,
      OTHER_SESSION,
    ]);

    await page.goto("/settings/sessions");

    const main = page.getByRole("main");
    // Per-row Revoke for the other device.
    await expect(main.getByRole("button", { name: /^revoke$/i })).toBeVisible({
      timeout: 10_000,
    });
    // Bulk affordance because an active non-current session exists.
    await expect(
      main.getByRole("button", { name: /sign out everywhere else/i }),
    ).toBeVisible();
  });

  test("DELETEs the chosen session when Revoke is clicked", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/sessions/me", [
      CURRENT_SESSION,
      OTHER_SESSION,
    ]);
    // Void endpoint: return JSON-parseable "null" (api-client calls
    // response.json() on any 200 with a JSON content-type).
    await mockJsonResponse(page, "**/api/v1/identity/sessions/sess-other", "null", {
      method: "DELETE",
    });

    await page.goto("/settings/sessions");

    const main = page.getByRole("main");
    const revoke = main.getByRole("button", { name: /^revoke$/i });
    await expect(revoke).toBeVisible({ timeout: 10_000 });

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/sessions/sess-other") &&
        r.method() === "DELETE",
      { timeout: 5_000 },
    );
    await revoke.click();
    await reqPromise;

    await expect(page.getByText(/session revoked/i)).toBeVisible();
  });

  test("renders the empty state when there are no sessions", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/sessions/me", []);

    await page.goto("/settings/sessions");

    const main = page.getByRole("main");
    await expect(main.getByText(/Active sessions/)).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText(/no active sessions found/i)).toBeVisible();
    // No bulk sign-out button when nothing is active.
    await expect(
      main.getByRole("button", { name: /sign out everywhere else/i }),
    ).toHaveCount(0);
  });
});
