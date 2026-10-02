import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// Only en-US ships in the main bundle; pt-BR is one lazy chunk (src/locales/pt-BR.ts). That makes
// two failure modes possible that a static import could not have: a pt-BR user seeing English
// until the chunk lands, and a switch whose chunk never arrives. In dev the "chunk" is the module
// request for src/locales/pt-BR.ts and its JSON files, which is what the abort below matches.
const PT_BR_CATALOG = /\/src\/locales\/pt-BR/;

test.describe("lazy pt-BR catalog", () => {
  test("a pt-BR visitor gets Portuguese on the first render, never English first", async ({ page }) => {
    // Record every state the app root goes through, from before the app's own scripts run.
    await page.addInitScript(() => {
      const w = window as unknown as { __sawEnglish: boolean };
      w.__sawEnglish = false;
      new MutationObserver(() => {
        if (document.getElementById("root")?.textContent?.includes("fullstackhero Administration")) {
          w.__sawEnglish = true;
        }
      }).observe(document, { childList: true, subtree: true, characterData: true });
    });
    // A slow catalog is what turns a missing await into a visible English first paint; on a fast
    // local dev server the lazy route chunk alone can hide it.
    await page.route(PT_BR_CATALOG, async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 750));
      await route.continue();
    });

    await page.goto("/login?culture=pt-BR");

    await expect(page.getByText("Administração fullstackhero").first()).toBeVisible();
    expect(await page.evaluate(() => (window as unknown as { __sawEnglish: boolean }).__sawEnglish)).toBe(false);
  });

  test("a switch whose catalog fails to load stays in English and saves nothing", async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);

    let profilePuts = 0;
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() === "PUT") profilePuts++;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "u-test-1",
          firstName: "Alice",
          lastName: "Nguyen",
          phoneNumber: "",
          email: TEST_USER.email,
          isActive: true,
          emailConfirmed: true,
          locale: "en-US",
        }),
      });
    });
    await page.route(PT_BR_CATALOG, (route) => route.abort());

    await page.goto("/?culture=en-US");
    await page.getByRole("button", { name: /open profile menu/i }).click();
    await page.getByRole("menuitem", { name: "Português (BR)" }).click();

    await expect(page.getByText("Language not changed")).toBeVisible();
    await expect(page.locator("html")).toHaveAttribute("lang", "en-US");
    expect(await page.evaluate(() => localStorage.getItem("fsh.dashboard.lng"))).toBe("en-US");
    expect(profilePuts).toBe(0);
  });
});
