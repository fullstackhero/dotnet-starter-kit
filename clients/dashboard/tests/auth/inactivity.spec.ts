import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// Drive the inactivity feature with tiny durations injected via /config.json:
// idle 2s → a 3s warning countdown → auto sign-out. The guard mounts in the
// authenticated AppShell, so any protected page exercises it.
const IDLE_MS = 2_000;
const WARNING_MS = 3_000;

test.beforeEach(async ({ page }) => {
  await page.route("**/config.json", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        apiBase: "",
        defaultTenant: "root",
        demoMode: false,
        inactivityIdleMs: IDLE_MS,
        inactivityWarningMs: WARNING_MS,
      }),
    }),
  );
  await seedAuthedSession(page, TEST_USER);
  // Defensive catch-all so a stray protected call can't 401→logout and race the
  // idle timer; the specific shell mocks register after this and win.
  await page.route("**/api/v1/**", (route) =>
    route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "[]" }),
  );
  await installShellMocks(page);
});

test.describe("inactivity auto-logout", () => {
  test("shows the warning modal after the idle threshold", async ({ page }) => {
    await page.goto("/settings/security");
    await expect(page.getByRole("dialog").getByText("Still there?")).toBeVisible({
      timeout: 9_000,
    });
    await expect(page.getByRole("button", { name: "I'm here" })).toBeVisible();
  });

  test("'I'm here' dismisses the warning and keeps the session", async ({ page }) => {
    await page.goto("/settings/security");
    const stay = page.getByRole("button", { name: "I'm here" });
    await expect(stay).toBeVisible({ timeout: 9_000 });

    await stay.click();

    await expect(page.getByText("Still there?")).toBeHidden();
    await expect(page).not.toHaveURL(/\/login$/);
  });

  test("signs out to /login with a notice when the countdown elapses", async ({ page }) => {
    await page.goto("/settings/security");
    await expect(page.getByRole("button", { name: "I'm here" })).toBeVisible({ timeout: 9_000 });

    await expect(page).toHaveURL(/\/login$/, { timeout: WARNING_MS + 6_000 });
    await expect(page.getByText(/signed out due to inactivity/i)).toBeVisible();
  });
});
