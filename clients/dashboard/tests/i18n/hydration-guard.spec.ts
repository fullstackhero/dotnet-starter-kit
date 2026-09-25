import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// A stale server locale must not yank the language out from under an explicit in-session
// choice.
//
// Every profile write refetches the SAME ["identity","me"] key the topbar reads, and that
// refetch can land before the new locale does. Before the hydration guard, the topbar's effect
// then saw persistedLocale change and called i18n.changeLanguage on it — the user watched the
// UI revert to English with no error and nothing to act on.
//
// The lost update itself is closed server-side by If-Match on PUT /identity/profile; what this
// pins is that the UI never follows a server value that lags an in-session choice.

/** The server never learns about the switch: every GET keeps answering en-US. */
const STALE_PROFILE = {
  id: TEST_USER.sub,
  firstName: "Alice",
  lastName: "Nguyen",
  phoneNumber: "",
  email: TEST_USER.email,
  isActive: true,
  emailConfirmed: true,
  locale: "en-US",
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);

  await page.route("**/api/v1/identity/profile", async (route) => {
    if (route.request().method() === "PUT") {
      await route.fulfill({ status: 200 });
      return;
    }
    if (route.request().method() === "GET") {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(STALE_PROFILE),
      });
      return;
    }
    await route.fallback();
  });

  await page.route("**/api/v1/identity/token/refresh", async (route) => {
    await route.fulfill({ status: 500 });
  });
});

test.describe("language hydration guard", () => {
  // index.html ships a static lang="en". Without syncing it, a Portuguese UI still declares
  // itself English to screen readers, browser translation and hyphenation — the document
  // language silently contradicts every visible string. Doubles as the one language signal in
  // this app that a stale render cannot satisfy, which is why the assertions above use it.
  test("the document language attribute follows the active locale", async ({ page }) => {
    const html = page.locator("html");

    await page.goto("/");
    await expect(html).toHaveAttribute("lang", "en-US");

    await page.getByRole("button", { name: /open profile menu/i }).click();
    await page.getByRole("menuitem", { name: "Português (BR)" }).click();

    await expect(html).toHaveAttribute("lang", "pt-BR");
  });

  test("still hydrates from the server on a fresh session with no in-session choice", async ({
    page,
  }) => {
    // The guard must not break the feature it protects: a locale chosen on another device
    // still carries over, because nothing was chosen in THIS session.
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() === "GET") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ ...STALE_PROFILE, locale: "pt-BR" }),
        });
        return;
      }
      await route.fallback();
    });

    await page.goto("/");

    await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");
  });
});
