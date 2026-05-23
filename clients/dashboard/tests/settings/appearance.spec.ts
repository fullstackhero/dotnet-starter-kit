import { expect, test } from "@playwright/test";
import { installShellMocks } from "../helpers/shell-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

// Appearance is entirely client-side — the ThemeProvider toggles the
// `dark` class on <html> and persists choices to localStorage. No API
// is hit beyond the shell defaults, so installShellMocks is enough.
test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("settings/appearance — theme + accent (client-side)", () => {
  test("renders the theme, accent, font, and density sections", async ({ page }) => {
    await page.goto("/settings/appearance");

    // CardTitle is a styled <div>, not a heading — assert on the text.
    await expect(page.getByText("Theme", { exact: true })).toBeVisible();
    await expect(page.getByText("Accent", { exact: true })).toBeVisible();
    await expect(page.getByText("Font", { exact: true })).toBeVisible();
    await expect(page.getByText("Density", { exact: true })).toBeVisible();

    // The three theme swatches are buttons with descriptive aria-labels.
    await expect(page.getByRole("button", { name: "Light theme" })).toBeVisible();
    await expect(page.getByRole("button", { name: "System theme" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Dark theme" })).toBeVisible();
  });

  test("selecting Dark applies dark mode; Light reverts it", async ({ page }) => {
    await page.goto("/settings/appearance");
    const html = page.locator("html");

    await page.getByRole("button", { name: "Dark theme" }).click();
    await expect(html).toHaveClass(/dark/);
    // The chosen mode is the active swatch (aria-pressed).
    await expect(page.getByRole("button", { name: "Dark theme" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );

    await page.getByRole("button", { name: "Light theme" }).click();
    await expect(html).not.toHaveClass(/dark/);
    await expect(page.getByRole("button", { name: "Light theme" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
  });

  test("persists the selected mode to localStorage", async ({ page }) => {
    await page.goto("/settings/appearance");

    await page.getByRole("button", { name: "Dark theme" }).click();
    const stored = await page.evaluate(() => window.localStorage.getItem("fsh.theme"));
    expect(stored).toBe("dark");
  });

  test("opens the custom accent dialog and applies a brand colour", async ({ page }) => {
    await page.goto("/settings/appearance");

    await page.getByRole("button", { name: "Custom accent" }).click();

    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: /pick your brand colour/i }),
    ).toBeVisible();
    await expect(dialog.getByLabel("Hue")).toBeVisible();
    await expect(dialog.getByLabel("Saturation")).toBeVisible();

    await dialog.getByRole("button", { name: /apply accent/i }).click();
    await expect(page.getByRole("dialog")).not.toBeVisible();
    // The custom accent is now the active swatch.
    await expect(page.getByRole("button", { name: "Custom accent" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
  });
});
