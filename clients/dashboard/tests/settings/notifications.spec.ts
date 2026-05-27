import { expect, test } from "@playwright/test";
import { installShellMocks } from "../helpers/shell-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

// Per-user notification preference persistence isn't built yet — the page
// is an honest placeholder that points users at the in-app bell. There's
// no preferences GET/PUT to mock beyond the shell defaults. Specs assert
// the placeholder copy + the bell affordance.
test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("settings/notifications — placeholder", () => {
  test("renders the preferences section and the placeholder copy", async ({ page }) => {
    await page.goto("/settings/notifications");

    // Section title is an <h3>; the sidebar nav uses plain spans, so the
    // heading role is unambiguous.
    await expect(
      page.getByRole("heading", { name: "Notification preferences" }),
    ).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /per-event preferences aren't tunable yet/i }),
    ).toBeVisible();
    await expect(page.getByText(/granular per-event email opt-ins/i)).toBeVisible();
  });

  test("exposes a button that opens the notifications bell", async ({ page }) => {
    await page.goto("/settings/notifications");

    await expect(
      page.getByRole("button", { name: /open notifications bell/i }),
    ).toBeVisible();
  });

  test("clicking the affordance stays on the notifications tab", async ({ page }) => {
    await page.goto("/settings/notifications");

    // The handler does an optional-chained `[data-notification-bell]?.click()`,
    // so it's a safe no-op when the topbar bell isn't mounted. We only assert
    // the click doesn't throw or navigate away from the settings tab.
    await page.getByRole("button", { name: /open notifications bell/i }).click();
    await expect(page).toHaveURL(/\/settings\/notifications$/);
  });
});
