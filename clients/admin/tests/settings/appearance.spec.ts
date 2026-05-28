import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// AppearanceSettings is purely client-side: it calls ThemeProvider.setTheme,
// which toggles the "dark" class on <html> and persists to the
// "fsh.admin.theme" localStorage key. No API to mock beyond the shell.

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("settings · appearance", () => {
  test("renders the Light and Dark theme options", async ({ page }) => {
    await page.goto("/settings/appearance");

    const main = page.getByRole("main");
    // "Theme" prose appears in the section description AND the settings nav
    // link ("Theme and visual preferences"); anchor to the SettingsSection <h2>.
    await expect(main.getByRole("heading", { name: "Theme" })).toBeVisible({ timeout: 10_000 });
    // Each mode is an aria-pressed <button>. The accessible name is built from
    // the label + blurb (+ "Active" when selected), so match the label as a
    // substring rather than anchoring to the start.
    await expect(main.getByRole("button", { name: /Light/ })).toBeVisible();
    await expect(main.getByRole("button", { name: /Dark/ })).toBeVisible();
  });

  test("selecting Dark applies dark mode and Light reverts it", async ({ page }) => {
    await page.goto("/settings/appearance");

    const main = page.getByRole("main");
    const dark = main.getByRole("button", { name: /Dark/ });
    const light = main.getByRole("button", { name: /Light/ });
    await expect(dark).toBeVisible({ timeout: 10_000 });

    // Force a known starting point regardless of the system color-scheme,
    // then assert each toggle drives the <html> class.
    await dark.click();
    await expect(page.locator("html")).toHaveClass(/dark/);
    await expect(dark).toHaveAttribute("aria-pressed", "true");

    await light.click();
    await expect(page.locator("html")).not.toHaveClass(/dark/);
    await expect(light).toHaveAttribute("aria-pressed", "true");
  });

  test("persists the selected theme to localStorage", async ({ page }) => {
    await page.goto("/settings/appearance");

    const main = page.getByRole("main");
    const light = main.getByRole("button", { name: /Light/ });
    await expect(light).toBeVisible({ timeout: 10_000 });

    await light.click();
    await expect(page.locator("html")).not.toHaveClass(/dark/);

    const stored = await page.evaluate(() => localStorage.getItem("fsh.admin.theme"));
    expect(stored).toBe("light");
  });
});
